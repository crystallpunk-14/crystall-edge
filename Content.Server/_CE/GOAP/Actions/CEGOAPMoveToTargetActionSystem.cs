using Content.Server._CE.ZLevels.LaddersCache;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.ZLevels.Core.Components;
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
    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<CEZMapComponent> _zMapQuery = default!;


    private readonly Dictionary<EntityUid, Direction> _pendingAscent = new();

    /// <summary>
    /// Tracks NPCs navigating to a descent point.
    /// Stores the slope direction and the map-below entity UID, captured at registration time,
    /// so the teleport doesn't need to re-resolve MapBelow at update time.
    /// </summary>
    private readonly Dictionary<EntityUid, (Direction SlopeDir, EntityUid BelowMapUid)> _pendingDescent = new();

    protected override void OnActionStartup(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionStartupEvent<CEGOAPMoveToTargetAction> args)
    {
        RegisterSteering(ent, args.Action);
    }

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPMoveToTargetAction> args)
    {
        if (!TryResolveCoords(ent, args.Action.Selector, out var coords))
            return;


        if (!_xformQuery.TryGetComponent(ent, out var npcXform))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        // If on different maps, we are doing cross-Z navigation — never report Finished directly.
        var sameMaps = npcXform.MapUid == _transform.GetMap(coords);

        // Re-register steering if target has moved significantly
        if (_steeringQuery.TryComp(ent, out var steering))
        {
            // Re-register if target moved significantly (only for same-map direct nav)
            if (sameMaps && steering.Coordinates.TryDistance(EntityManager, coords, out var delta)
                         && delta > args.Action.ReregisterThreshold)
            {
                var comp = _steering.Register(ent, coords);
                comp.Range = args.Action.Range;
            }

            switch (steering.Status)
            {
                case SteeringStatus.InRange:
                    if (sameMaps)
                    {
                        args.Status = CEGOAPActionStatus.Finished;
                        return;
                    }

                    // Reached slope destination — teleport between Z-levels
                    if (_pendingAscent.Remove(ent.Owner, out var ascentDir))
                    {
                        _zLevels.TryMoveUp(ent);
                        // ascentDir = downhill; shift UPHILL (opposite) to land on upper map floor
                        var pos = _transform.GetWorldPosition(ent);
                        _transform.SetWorldPosition(ent, pos + ascentDir.GetOpposite().ToVec() * 0.25f);
                        // ParentChanged triggers re-plan
                    }
                    else if (_pendingDescent.Remove(ent.Owner, out var descentData))
                    {
                        // Force move to the map below at the shifted position.
                        // BelowMapUid was captured at registration time — no nullable lookup needed.
                        if (_mapQuery.TryComp(descentData.BelowMapUid, out var belowMapComp))
                        {
                            var pos = _transform.GetWorldPosition(ent);
                            var newPos = pos + descentData.SlopeDir.ToVec() * 0.75f;
                            _transform.SetMapCoordinates(ent, new MapCoordinates(newPos, belowMapComp.MapId));
                        }
                        // ParentChanged triggers re-plan
                    }
                    else
                    {
                        // Neither entry found — state de-synced (e.g. TryMoveUp failed silently).
                        // Re-register steering so the NPC tries again next tick.
                        RegisterSteering(ent, args.Action);
                    }

                    break;
                case SteeringStatus.NoPath:
                    args.Status = CEGOAPActionStatus.Failed;
                    return;
            }
        }

        args.Status = CEGOAPActionStatus.Running;
    }

    protected override void OnActionShutdown(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionShutdownEvent<CEGOAPMoveToTargetAction> args)
    {
        _pendingAscent.Remove(ent.Owner);
        _pendingDescent.Remove(ent.Owner);
        _steering.Unregister(ent);
    }

    private void RegisterSteering(Entity<CEGOAPComponent> ent, CEGOAPMoveToTargetAction action)
    {
        if (!TryResolveCoords(ent, action.Selector, out var coords))
            return;

        if (!_xformQuery.TryGetComponent(ent, out var npcXform))
            return;

        var npcMapUid = npcXform.MapUid;
        var targetMapUid = _transform.GetMap(coords);
        if (npcMapUid == null || targetMapUid == null)
            return;


        // Same map — direct steering to target
        if (npcMapUid == targetMapUid)
        {
            _pendingAscent.Remove(ent.Owner, out _);
            _pendingDescent.Remove(ent.Owner, out _);
            var comp = _steering.Register(ent, coords);
            comp.Range = action.Range;
            return;
        }

        // Different maps — compute Z-direction
        var zOffset = GetZOffset(npcMapUid.Value, targetMapUid.Value);
        if (zOffset == 0)
            return;

        var npcWorldPos = _transform.GetWorldPosition(npcXform);


        if (zOffset > 0) //Finds the nearest slope on the current map and steers to its uphill edge.
        {
            if (!_gridQuery.TryGetComponent(npcMapUid, out var grid))
                return;

            if (!_ladderCache.GetNearestLadder(npcMapUid.Value, npcWorldPos, 5, out var slopeTilePos, out var cachedSlope))
                return;

            // cachedSlope.Direction = downhill. Uphill = GetOpposite().
            // Steer to the uphill edge of the slope tile (the border where height reaches 1.0).
            var uphillDir = cachedSlope.Direction.GetOpposite();
            var slopeTileCenter = _mapSystem.GridTileToLocal(npcMapUid.Value, grid, slopeTilePos);
            var edgeOffset = uphillDir.ToVec() * 0.45f;
            var targetCoords = new EntityCoordinates(slopeTileCenter.EntityId,
                slopeTileCenter.Position + edgeOffset);

            var comp = _steering.Register(ent, targetCoords);
            comp.Range = 0.3f;

            _pendingAscent[ent.Owner] = cachedSlope.Direction;
            _pendingDescent.Remove(ent.Owner);
        }
        else //Finds the nearest slope on the map below and locates a walkable tile on the current map
        {
            if (!_gridQuery.TryGetComponent(npcMapUid, out var grid))
                return;

            if (!_zLevels.TryMapDown(npcMapUid.Value, out var belowMap))
                return;

            // Find the nearest slope on the map below — world coords are shared across Z-levels.
            if (!_ladderCache.GetNearestLadder(belowMap, npcWorldPos, 5, out var slopeTilePos, out var cachedSlope))
                return;

            var uphillDir = cachedSlope.Direction.GetOpposite();
            var approachTile = slopeTilePos + uphillDir.ToIntVec();

            if (!_mapSystem.TryGetTileRef(npcMapUid.Value, grid, approachTile, out var tileRef) || tileRef.Tile.IsEmpty)
                return;

            // Steer to the edge of the target tile closest to the slope (= downhill edge).
            var tileCenter = _mapSystem.GridTileToLocal(npcMapUid.Value, grid, approachTile);
            var edgeOffset = cachedSlope.Direction.ToVec() * 0.4f;
            var targetCoords = new EntityCoordinates(tileCenter.EntityId,
                tileCenter.Position + edgeOffset);

            var comp = _steering.Register(ent, targetCoords);
            comp.Range = 0.3f;

            _pendingDescent[ent.Owner] = (cachedSlope.Direction, belowMap.Owner);
            _pendingAscent.Remove(ent.Owner);
        }
    }

    /// <summary>
    /// Computes the Z-offset from the NPC's map to the target's map.
    /// Returns positive if target is above, negative if below, 0 if not in the same Z-network.
    /// </summary>
    private int GetZOffset(EntityUid npcMapUid, EntityUid targetMapUid)
    {
        if (!_zMapQuery.TryGetComponent(npcMapUid, out var npcZMap))
            return 0;

        if (!_zMapQuery.TryGetComponent(targetMapUid, out var targetZMap))
            return 0;

        // Reject cross-network routing: maps must be in the same Z-network.
        if (!_zLevels.TryGetMapNetwork(npcMapUid, out var npcNetwork) ||
            !_zLevels.TryGetMapNetwork(targetMapUid, out var targetNetwork) ||
            npcNetwork.Owner != targetNetwork.Owner)
            return 0;

        return targetZMap.Depth - npcZMap.Depth;
    }
}
