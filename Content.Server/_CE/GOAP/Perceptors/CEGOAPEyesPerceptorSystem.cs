using System.Numerics;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Perceptors;

/// <summary>
/// Vision-based perception. Periodically remembers entities passing <see cref="Whitelist"/>
/// that are within radius and line of sight. Classification is the responsibility of higher layers.
/// Optionally extends scanning to adjacent Z-levels via tile-transparency gating.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPEyesPerceptorComponent : Component
{
    /// <summary>
    /// What the eyes can see.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

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
    public bool CrossZLevelVision = true;

    [ViewVariables]
    public TimeSpan NextUpdateTime;
}

public sealed partial class CEGOAPEyesPerceptorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;
    [Dependency] private CEGOAPSystem _goap = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;

    private readonly HashSet<EntityUid> _nearbyBuffer = new();

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
        ScanMap(ent, xform.MapID, selfPos, xform.MapID, null);

        if (!ent.Comp1.CrossZLevelVision || xform.MapUid is not { } currentMap)
            return;

        // Below: line of sight is checked on our map, and the tile above the target must let light through.
        if (_zLevels.TryMapDown((currentMap, null), out var mapBelow) &&
            _mapQuery.TryComp(mapBelow, out var belowMap))
        {
            ScanMap(ent, belowMap.MapId, selfPos, xform.MapID, currentMap);
        }

        // Above: only while we stand under a transparent tile; line of sight is checked on the map above.
        if (_zLevels.TryMapUp((currentMap, null), out var mapAbove) &&
            IsTileTransparentAt(mapAbove, selfPos) &&
            _mapQuery.TryComp(mapAbove, out var aboveMap))
        {
            ScanMap(ent, aboveMap.MapId, selfPos, aboveMap.MapId, null);
        }
    }

    /// <summary>
    /// Remembers whitelisted entities on <paramref name="lookMap"/> around <paramref name="selfPos"/>
    /// that are visible along a line of sight traced on <paramref name="losMap"/>.
    /// </summary>
    /// <param name="ceilingMap">If set, a target is only seen through a transparent tile above it on this map.</param>
    private void ScanMap(
        Entity<CEGOAPEyesPerceptorComponent, CEGOAPComponent> ent,
        MapId lookMap,
        Vector2 selfPos,
        MapId losMap,
        EntityUid? ceilingMap)
    {
        var (uid, eyes, goap) = ent;
        var sameMap = _transform.GetMapId(uid) == lookMap;

        _nearbyBuffer.Clear();
        _lookup.GetEntitiesInRange(lookMap, selfPos, eyes.VisionRadius, _nearbyBuffer);

        foreach (var target in _nearbyBuffer)
        {
            if (target == uid || Terminating(target) || !_whitelist.IsValid(eyes.Whitelist, target))
                continue;

            if (!_xformQuery.TryGetComponent(target, out var targetXform))
                continue;

            var targetPos = _transform.GetWorldPosition(targetXform);
            if (Vector2.Distance(selfPos, targetPos) > eyes.VisionRadius)
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

            _goap.Remember((uid, goap), target, targetXform.Coordinates);
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
