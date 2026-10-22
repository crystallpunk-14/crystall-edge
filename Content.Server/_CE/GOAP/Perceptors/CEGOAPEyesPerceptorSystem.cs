using System.Numerics;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Perceptors;

/// <summary>
/// Vision-based perception. Periodically remembers entities having any of <see cref="Components"/>
/// that are within radius and line of sight. Classification is the responsibility of higher layers.
/// Optionally extends scanning to adjacent Z-levels via tile-transparency gating.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPEyesPerceptorComponent : Component
{
    /// <summary>
    /// What the eyes can see: entities having any of these components.
    /// Looked up per component type, so keep the list to what the agent actually cares about.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Detection range in tiles.
    /// </summary>
    [DataField]
    public float VisionRadius = 10f;

    /// <summary>
    /// How often the perceptor re-scans.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// When true, the perceptor also scans the adjacent Z-level above and below.
    /// Entities on the map below are only seen through a transparent tile above them on the agent's map.
    /// Entities on the map above are only seen while the agent stands under a transparent tile.
    /// </summary>
    [DataField]
    public bool CrossZLevelVision;

    /// <summary>
    /// How long a seen entity is remembered. Falls back to the agent's memory duration if null.
    /// </summary>
    [DataField]
    public TimeSpan? MemoryDuration;

    [ViewVariables]
    public TimeSpan NextUpdateTime;
}

public sealed partial class CEGOAPEyesPerceptorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;
    [Dependency] private CEGOAPSystem _goap = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;

    private readonly HashSet<Entity<IComponent>> _lookupBuffer = new();
    private readonly HashSet<EntityUid> _nearbyBuffer = new();

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEGOAPEyesPerceptorComponent> ent, ref MapInitEvent args)
    {
        // Spread scans over the interval so agents spawned together don't all scan on the same tick.
        ent.Comp.NextUpdateTime = _timing.CurTime + ent.Comp.UpdateInterval * _random.NextDouble();
    }

    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<CEGOAPEyesPerceptorComponent, CEGOAPComponent, CEActiveGOAPComponent>();
        while (query.MoveNext(out var uid, out var eyes, out var goap, out _))
        {
            if (curTime < eyes.NextUpdateTime)
                continue;

            eyes.NextUpdateTime = curTime + eyes.UpdateInterval;
            Scan((uid, eyes, goap));
        }
    }

    private void Scan(Entity<CEGOAPEyesPerceptorComponent, CEGOAPComponent> ent)
    {
        if (!_xformQuery.TryGetComponent(ent, out var xform))
            return;

        var selfPos = _transform.GetWorldPosition(xform);
        ScanMap(ent, xform.MapID, selfPos, xform.MapID, null, true);

        if (!ent.Comp1.CrossZLevelVision || xform.MapUid is not { } currentMap)
            return;

        // Below: line of sight is checked on our map, and the tile above the target must let light through.
        if (_zLevels.TryMapDown((currentMap, null), out var mapBelow) &&
            _mapQuery.TryComp(mapBelow, out var belowMap))
        {
            ScanMap(ent, belowMap.MapId, selfPos, xform.MapID, currentMap, false);
        }

        // Above: only while we stand under a transparent tile; line of sight is checked on the map above.
        if (_zLevels.TryMapUp((currentMap, null), out var mapAbove) &&
            IsTileTransparentAt(mapAbove, selfPos) &&
            _mapQuery.TryComp(mapAbove, out var aboveMap))
        {
            ScanMap(ent, aboveMap.MapId, selfPos, aboveMap.MapId, null, false);
        }
    }

    /// <summary>
    /// Remembers entities on <paramref name="lookMap"/> around <paramref name="selfPos"/> having any of
    /// <see cref="CEGOAPEyesPerceptorComponent.Components"/> that are visible along a line of sight traced on <paramref name="losMap"/>.
    /// </summary>
    /// <param name="ceilingMap">If set, a target is only seen through a transparent tile above it on this map.</param>
    /// <param name="sameMap">True when <paramref name="lookMap"/> is the agent's own map.</param>
    private void ScanMap(
        Entity<CEGOAPEyesPerceptorComponent, CEGOAPComponent> ent,
        MapId lookMap,
        Vector2 selfPos,
        MapId losMap,
        EntityUid? ceilingMap,
        bool sameMap)
    {
        var (uid, eyes, goap) = ent;
        var radiusSquared = eyes.VisionRadius * eyes.VisionRadius;

        // Per-type lookups walk only entities with the component instead of everything in range.
        _nearbyBuffer.Clear();
        foreach (var registration in eyes.Components.Values)
        {
            _lookupBuffer.Clear();
            _lookup.GetEntitiesInRange(registration.Component.GetType(), lookMap, selfPos, eyes.VisionRadius, _lookupBuffer);

            foreach (var found in _lookupBuffer)
            {
                _nearbyBuffer.Add(found.Owner);
            }
        }

        foreach (var target in _nearbyBuffer)
        {
            if (target == uid || !_xformQuery.TryGetComponent(target, out var targetXform))
                continue;

            var targetPos = _transform.GetWorldPosition(targetXform);
            if (Vector2.DistanceSquared(selfPos, targetPos) > radiusSquared || Terminating(target))
                continue;

            var visible = sameMap
                ? _examine.InRangeUnOccluded(uid, target, eyes.VisionRadius + 0.5f)
                : _examine.InRangeUnOccluded(
                    new MapCoordinates(selfPos, losMap),
                    new MapCoordinates(targetPos, losMap),
                    eyes.VisionRadius + 0.5f,
                    null);

            if (!visible)
                continue;

            if (ceilingMap is { } ceiling && !IsTileTransparentAt(ceiling, targetPos))
                continue;

            _goap.Remember((uid, goap), target, targetXform.Coordinates, eyes.MemoryDuration);
        }
    }

    /// <summary>
    /// Returns true if the tile at the given world position on the specified map is transparent
    /// (open air, empty tile, or a tile with <see cref="ContentTileDefinition.Transparent"/> set).
    /// </summary>
    private bool IsTileTransparentAt(EntityUid mapUid, Vector2 worldPos)
    {
        if (!_gridQuery.TryComp(mapUid, out var grid))
            return true; // No grid = open air = transparent

        if (!_mapSystem.TryGetTileRef(mapUid, grid, worldPos, out var tileRef))
            return true; // No tile = transparent

        if (tileRef.Tile.IsEmpty)
            return true;

        var tileDef = (ContentTileDefinition) _tileDef[tileRef.Tile.TypeId];
        return tileDef.Transparent;
    }
}
