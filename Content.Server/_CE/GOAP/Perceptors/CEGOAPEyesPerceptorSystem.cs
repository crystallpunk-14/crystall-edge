using System.Numerics;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Perceptors;

/// <summary>
/// Vision-based perception. Periodically scans living mobs in radius with line-of-sight
/// and feeds them into the GOAP knowledge store without any faction filtering — classification
/// (friend/foe/etc.) is the responsibility of higher layers.
/// Optionally extends scanning to adjacent Z-levels via tile-transparency gating.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPEyesPerceptorComponent : Component
{
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
    /// When true, the perceptor also scans mobs on the adjacent Z-level above and below.
    /// Targets on the map below are only detected if there is a transparent tile above them
    /// on the NPC's map. Targets on the map above are only detected if the NPC stands on a
    /// transparent tile.
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
    // CrystallEdge: Rogue used CEMobStateSystem (CE-only). This fork has no CE health stack,
    // so use vanilla MobStateSystem instead.
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    private readonly HashSet<Entity<NpcFactionMemberComponent>> _nearbyBuffer = new();

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
        var (uid, eyes, goap) = ent;

        if (!_xformQuery.TryGetComponent(uid, out var xform))
            return;

        var selfWorldPos = _transform.GetWorldPosition(xform);
        var currentMapUid = xform.MapUid;

        // --- Same-map scan (with LOS) ---
        // Scan all faction members: covers both player mobs (MobStateComponent) and CE NPC mobs.
        // Alive check: players use MobStateComponent; NPCs without it are alive while non-terminating.
        _nearbyBuffer.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, eyes.VisionRadius, _nearbyBuffer);

        foreach (var target in _nearbyBuffer)
        {
            var targetUid = target.Owner;
            if (targetUid == uid)
                continue;

            if (_mobStateQuery.TryGetComponent(targetUid, out var mobState)
                ? _mobState.IsIncapacitated(targetUid, mobState)
                : Terminating(targetUid))
                continue;

            if (!_xformQuery.TryGetComponent(targetUid, out var targetXform))
                continue;

            var targetWorldPos = _transform.GetWorldPosition(targetXform);
            if (Vector2.Distance(selfWorldPos, targetWorldPos) > eyes.VisionRadius)
                continue;

            if (!_examine.InRangeUnOccluded(uid, targetUid, eyes.VisionRadius + 0.5f))
                continue;

            _goap.Remember((uid, goap), targetUid, targetXform.Coordinates);
        }

        if (!eyes.CrossZLevelVision || currentMapUid == null)
            return;

        if (!_mapQuery.TryComp(currentMapUid.Value, out var currentMapComp))
            return;

        // Second - search map below and filter targets with transparent tile above their head.
        // LOS is checked on our current map; then we verify the floor tile
        // directly above the target (on our map) is transparent — i.e. there is a hole/grate
        // in the ceiling the NPC can look through.
        if (_zLevels.TryMapDown((currentMapUid.Value, null), out var mapBelow) &&
            _mapQuery.TryComp(mapBelow, out var belowMapComp))
        {
            _nearbyBuffer.Clear();
            _lookup.GetEntitiesInRange(
                new MapCoordinates(selfWorldPos, belowMapComp.MapId),
                eyes.VisionRadius,
                _nearbyBuffer);

            foreach (var target in _nearbyBuffer)
            {
                var targetUid = target.Owner;
                if (targetUid == uid)
                    continue;

                if (TryComp<MobStateComponent>(targetUid, out var mobState)
                    ? _mobState.IsIncapacitated(targetUid, mobState)
                    : Terminating(targetUid))
                    continue;

                if (!_xformQuery.TryGetComponent(targetUid, out var targetXform))
                    continue;

                var targetWorldPos = _transform.GetWorldPosition(targetXform);
                if (Vector2.Distance(selfWorldPos, targetWorldPos) > eyes.VisionRadius)
                    continue;

                // LOS check — both points on our current map. We verify there is a direct line
                // of sight from us to the point on our level where the target below is located.
                var selfOnCurrentMap = new MapCoordinates(selfWorldPos, currentMapComp.MapId);
                var targetPosOnCurrentMap = new MapCoordinates(targetWorldPos, currentMapComp.MapId);
                if (!_examine.InRangeUnOccluded(selfOnCurrentMap, targetPosOnCurrentMap, eyes.VisionRadius + 0.5f, null))
                    continue;

                // Only visible if the tile directly above the target (on our map) is transparent:
                // i.e. the floor/ceiling between the two levels lets light through.
                if (!IsTileTransparentAt(currentMapUid.Value, targetWorldPos))
                    continue;

                _goap.Remember((uid, goap), targetUid, targetXform.Coordinates);
            }
        }

        // Third - if we have a transparent tile above us, search map above.
        // The transparent tile check gates the entire scan; LOS is then checked per-target
        // on the above map's coordinate space.
        if (_zLevels.TryMapUp((currentMapUid.Value, null), out var mapAbove) &&
            IsTileTransparentAt(mapAbove, selfWorldPos) &&
            _mapQuery.TryComp(mapAbove, out var aboveMapComp))
        {
            _nearbyBuffer.Clear();
            _lookup.GetEntitiesInRange(
                new MapCoordinates(selfWorldPos, aboveMapComp.MapId),
                eyes.VisionRadius,
                _nearbyBuffer);

            foreach (var target in _nearbyBuffer)
            {
                var targetUid = target.Owner;
                if (targetUid == uid)
                    continue;

                if (TryComp<MobStateComponent>(targetUid, out var mobState)
                    ? _mobState.IsIncapacitated(targetUid, mobState)
                    : Terminating(targetUid))
                    continue;

                if (!_xformQuery.TryGetComponent(targetUid, out var targetXform))
                    continue;

                var targetWorldPos = _transform.GetWorldPosition(targetXform);
                if (Vector2.Distance(selfWorldPos, targetWorldPos) > eyes.VisionRadius)
                    continue;

                // LOS check — both points projected onto the above map.
                var selfOnAboveMap = new MapCoordinates(selfWorldPos, aboveMapComp.MapId);
                var targetOnAboveMap = new MapCoordinates(targetWorldPos, aboveMapComp.MapId);
                if (!_examine.InRangeUnOccluded(selfOnAboveMap, targetOnAboveMap, eyes.VisionRadius + 0.5f, null))
                    continue;

                _goap.Remember((uid, goap), targetUid, targetXform.Coordinates);
            }
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
