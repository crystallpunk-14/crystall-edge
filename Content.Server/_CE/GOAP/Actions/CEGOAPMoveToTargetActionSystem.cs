using Content.Server._CE.ZLevels.LaddersCache;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>
/// Moves the NPC towards its current target entity.
/// Uses absolute grid coordinates for proper pathfinding (avoiding space tiles).
/// Only re-registers steering when the target moves significantly.
/// </summary>
public sealed partial class CEGOAPMoveToTargetAction : CEGOAPActionBase<CEGOAPMoveToTargetAction>
{
    /// <summary>
    /// How close the NPC needs to get to the target to consider the action complete.
    /// </summary>
    [DataField]
    public float Range = 1f;

    /// <summary>
    /// How far the target must move before re-registering the steering destination.
    /// Prevents constant pathfinding recalculation while still tracking moving targets.
    /// </summary>
    [DataField]
    public float ReregisterThreshold = 1f;
}

[RegisterComponent]
public sealed partial class CEGOAPMoveToTargetComponent : Component
{
    /// <summary>
    /// Target identity of the current steering request. A selector retarget must
    /// not apply the previous target's terminal steering status to the new one.
    /// </summary>
    public EntityUid? Target;

    /// <summary>
    /// The selected slope and adjacent map, captured when steering starts.
    /// Arrival must still use this anchored slope, not a deleted or replaced
    /// cache entry. Consume the snapshot before a map change stops the action.
    /// </summary>
    public (EntityUid Slope, EntityUid DestinationMap, Vector2i Tile, Direction Direction)? PendingTransition;
}

public sealed partial class CEGOAPMoveToTargetActionSystem : CEGOAPActionSystem<CEGOAPMoveToTargetAction>
{
    [Dependency] private NPCSteeringSystem _steering = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private CEZLevelsLaddersCacheSystem _ladderCache = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<NPCSteeringComponent> _steeringQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    protected override void OnActionStartup(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionStartupEvent<CEGOAPMoveToTargetAction> args)
    {
        var state = EnsureComp<CEGOAPMoveToTargetComponent>(ent);
        if (TryResolveCoords(ent, args.Action.Selector, out var coords, out state.Target))
            RegisterSteering(ent, args.Action, coords, state);
    }

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPMoveToTargetAction> args)
    {
        if (!TryResolveCoords(ent, args.Action.Selector, out var coords, out args.Target))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        if (!_xformQuery.TryGetComponent(ent, out var npcXform))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        // If on different maps, we are doing cross-Z navigation — never report Finished directly.
        var sameMaps = npcXform.MapUid == _transform.GetMap(coords);

        var state = EnsureComp<CEGOAPMoveToTargetComponent>(ent);
        var retargeted = state.Target != args.Target;
        state.Target = args.Target;

        // Re-register steering if target has moved significantly
        if (_steeringQuery.TryComp(ent, out var steering))
        {
            // Re-register if target moved significantly (only for same-map direct nav)
            if (retargeted || sameMaps &&
                steering.Coordinates.TryDistance(EntityManager, coords, out var delta) &&
                delta > args.Action.ReregisterThreshold)
            {
                // Register alone retains terminal status on an existing steering
                // component. Start a fresh request and let it run before reading
                // InRange/NoPath, regardless of whether target backoff is enabled.
                _steering.Unregister(ent);
                RegisterSteering(ent, args.Action, coords, state);
                return;
            }

            switch (steering.Status)
            {
                case SteeringStatus.InRange:
                    if (sameMaps)
                    {
                        args.Status = CEGOAPActionStatus.Finished;
                        return;
                    }

                    if (state.PendingTransition == null)
                    {
                        // A same-map target may have moved to another map while
                        // we approached it. Start a fresh cross-Z request.
                        _steering.Unregister(ent);
                        RegisterSteering(ent, args.Action, coords, state);
                        return;
                    }

                    // ParentChanged stops the action synchronously on success.
                    args.Status = TryTransition(ent, npcXform.MapUid, state)
                        ? CEGOAPActionStatus.Running
                        : CEGOAPActionStatus.Failed;
                    return;
                case SteeringStatus.NoPath:
                    args.Status = CEGOAPActionStatus.Failed;
                    return;
            }
        }
        else
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        args.Status = CEGOAPActionStatus.Running;
    }

    protected override void OnActionShutdown(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionShutdownEvent<CEGOAPMoveToTargetAction> args)
    {
        _steering.Unregister(ent);
        RemComp<CEGOAPMoveToTargetComponent>(ent);
    }

    private void RegisterSteering(
        Entity<CEGOAPComponent> ent,
        CEGOAPMoveToTargetAction action,
        EntityCoordinates coords,
        CEGOAPMoveToTargetComponent state)
    {
        state.PendingTransition = null;
        if (!_xformQuery.TryGetComponent(ent, out var npcXform))
            return;

        var npcMapUid = npcXform.MapUid;
        var targetMapUid = _transform.GetMap(coords);
        if (npcMapUid == null || targetMapUid == null)
            return;

        // Same map — direct steering to target
        if (npcMapUid == targetMapUid)
        {
            var comp = _steering.Register(ent, coords);
            comp.Range = action.Range;
            return;
        }

        // Different maps — compute Z-direction
        var zOffset = GetZOffset(npcMapUid.Value, targetMapUid.Value);
        if (zOffset == 0 ||
            !_zLevels.TryMapOffset(npcMapUid.Value, Math.Sign(zOffset), out var destinationMap) ||
            !_gridQuery.TryGetComponent(npcMapUid, out var grid))
            return;

        var npcWorldPos = _transform.GetWorldPosition(npcXform);
        var slopeMap = zOffset > 0 ? npcMapUid.Value : destinationMap.Owner;
        // World coordinates are shared across Z-levels. Ascend on the current
        // map's slope; descend via the slope on the adjacent map below.
        if (!_ladderCache.GetNearestLadder(slopeMap, npcWorldPos, 5, out var slopeTilePos, out var cachedSlope))
            return;

        var uphillDir = cachedSlope.Direction.GetOpposite();
        EntityCoordinates targetCoords;

        if (zOffset > 0) //Finds the nearest slope on the current map and steers to its uphill edge.
        {
            // cachedSlope.Direction = downhill. Uphill = GetOpposite().
            // Steer to the uphill edge of the slope tile (the border where height reaches 1.0).
            var slopeTileCenter = _mapSystem.GridTileToLocal(npcMapUid.Value, grid, slopeTilePos);
            var edgeOffset = uphillDir.ToVec() * 0.45f;
            targetCoords = new EntityCoordinates(slopeTileCenter.EntityId,
                slopeTileCenter.Position + edgeOffset);
        }
        else //Finds the nearest slope on the map below and locates a walkable tile on the current map
        {
            var approachTile = slopeTilePos + uphillDir.ToIntVec();

            if (!_mapSystem.TryGetTileRef(npcMapUid.Value, grid, approachTile, out var tileRef) || tileRef.Tile.IsEmpty)
                return;

            // Steer to the edge of the target tile closest to the slope (= downhill edge).
            var tileCenter = _mapSystem.GridTileToLocal(npcMapUid.Value, grid, approachTile);
            var edgeOffset = cachedSlope.Direction.ToVec() * 0.4f;
            targetCoords = new EntityCoordinates(tileCenter.EntityId,
                tileCenter.Position + edgeOffset);
        }

        var steering = _steering.Register(ent, targetCoords);
        steering.Range = 0.3f;
        state.PendingTransition = (cachedSlope.Entity, destinationMap.Owner, slopeTilePos, cachedSlope.Direction);
    }

    private bool TryTransition(EntityUid user, EntityUid? currentMap, CEGOAPMoveToTargetComponent state)
    {
        if (currentMap == null || state.PendingTransition is not { } pending)
            return false;

        // Map changes raise ParentChanged synchronously and remove this runtime
        // component. Keep only the local snapshot while executing the transition.
        state.PendingTransition = null;
        var offset = GetZOffset(currentMap.Value, pending.DestinationMap);
        if (offset is not (1 or -1) ||
            !_zLevels.TryMapOffset(currentMap.Value, offset, out var destination, out var destinationMap) ||
            destination.Owner != pending.DestinationMap)
            return false;

        var slopeMap = offset > 0 ? currentMap.Value : destination.Owner;
        if (TerminatingOrDeleted(pending.Slope) ||
            !_xformQuery.TryGetComponent(pending.Slope, out var slopeXform) ||
            !slopeXform.Anchored || slopeXform.GridUid != slopeMap ||
            !TryComp<CEZLevelsLaddersCacheComponent>(slopeMap, out var cache) ||
            !cache.Slopes.TryGetValue(pending.Tile, out var slope) ||
            slope.Entity != pending.Slope || slope.Direction != pending.Direction)
            return false;

        if (offset > 0)
        {
            if (!_zLevels.TryMoveUp(user))
                return false;

            // Direction is downhill; shift UPHILL to land on upper map floor,
            // but only after a successful ascent.
            var pos = _transform.GetWorldPosition(user);
            _transform.SetWorldPosition(user, pos + pending.Direction.GetOpposite().ToVec() * 0.25f);
        }
        else
        {
            // Force move to the validated map below at the shifted position.
            var pos = _transform.GetWorldPosition(user);
            var newPos = pos + pending.Direction.ToVec() * 0.75f;
            _transform.SetMapCoordinates(user, new MapCoordinates(newPos, destinationMap.MapId));
        }

        return true;
    }

    /// <summary>
    /// Computes the Z-offset from the NPC's map to the target's map.
    /// Returns positive if target is above, negative if below, 0 if not in the same Z-network.
    /// </summary>
    private int GetZOffset(EntityUid npcMapUid, EntityUid targetMapUid)
    {
        // Offset checks shared network identity, but not its lifetime. Reject
        // stale networks even when cached adjacent-map references still exist.
        if (!_zLevels.TryGetZLevelOffset(npcMapUid, targetMapUid, out var offset) ||
            !_zLevels.TryGetMapNetwork(npcMapUid, out _))
            return 0;

        return offset;
    }
}
