using System.Numerics;
using System.Threading;
using Content.Server._CE.MeleeWeapon;
using Content.Server._CE.ZLevels.LaddersCache;
using Content.Server.Administration.Managers;
using Content.Server.DoAfter;
using Content.Server.NPC.Pathfinding;
using Content.Server.Storage.EntitySystems;
using Content.Shared._CE.Animation.Core;
using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.CCVar;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Events;
using Content.Shared.NPC.Systems;
using Content.Shared.Physics;
using Content.Shared.Prying.Systems;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._CE.GOAP.Steering;

/// <summary>
/// Moves GOAP agents. Callers poll <see cref="Navigate"/> (or <see cref="Continue"/>) every tick; the system
/// frees the agent (unbuckle, unpull, escape storage), crosses Z-levels through slopes and steers along
/// pathfinding paths, handling obstacles on the way.
/// The steering core (context steering, obstacles) is a copy of the vanilla NPC steering, decoupled from HTN.
/// </summary>
public sealed partial class CEGOAPSteeringSystem : EntitySystem
{
    /*
     * We use context steering to determine which way to move.
     * This involves creating an array of possible directions and assigning a value for the desireability of each direction.
     * See http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter18_Context_Steering_Behavior-Driven_Steering_at_the_Macro_Scale.pdf
     */

    private const byte InterestDirections = SharedNPCSteeringSystem.InterestDirections;
    private const float InterestRadians = SharedNPCSteeringSystem.InterestRadians;

    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private CEWeaponSystem _weapon = default!;
    [Dependency] private CESharedAnimationActionSystem _animationAction = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private CEZLevelsLaddersCacheSystem _ladderCache = default!;
    [Dependency] private ClimbSystem _climb = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private PathfindingSystem _pathfindingSystem = default!;
    [Dependency] private PryingSystem _pryingSystem = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<BuckleComponent> _buckleQuery = default!;
    [Dependency] private EntityQuery<FixturesComponent> _fixturesQuery = default!;
    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<MovementSpeedModifierComponent> _modifierQuery = default!;
    [Dependency] private EntityQuery<NpcFactionMemberComponent> _factionQuery = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;
    [Dependency] private EntityQuery<PullableComponent> _pullableQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<CEZMapComponent> _zMapQuery = default!;

    private readonly ObjectPool<HashSet<EntityUid>> _entSetPool =
        new DefaultObjectPool<HashSet<EntityUid>>(new SetPolicy<EntityUid>());

    private static readonly Vector2[] Directions = new Vector2[InterestDirections];

    /// <summary>
    /// Admin sessions subscribed to the steering debug overlay.
    /// </summary>
    private readonly HashSet<ICommonSession> _subscribedSessions = new();

    private readonly List<(EntityUid Uid, CEGOAPSteeringComponent Steering, InputMoverComponent Mover, TransformComponent Xform)> _agents = new();

    private readonly List<EntityUid> _ignoredObstacles = new();

    private bool _enabled = true;

    public override void Initialize()
    {
        base.Initialize();

        for (var i = 0; i < InterestDirections; i++)
        {
            Directions[i] = new Angle(InterestRadians * i).ToVec();
        }

        UpdatesBefore.Add(typeof(SharedPhysicsSystem));
        Subs.CVar(_configManager, CCVars.CEGOAPEnabled, SetEnabled, true);

        SubscribeLocalEvent<CEGOAPSteeringComponent, ComponentShutdown>(OnSteeringShutdown);
        SubscribeNetworkEvent<RequestNPCSteeringDebugEvent>(OnDebugRequest);
    }

    #region API

    /// <summary>
    /// Moves the agent towards <paramref name="destination"/> until it is within <paramref name="range"/>.
    /// Poll every tick with the current destination; the path is only rebuilt when the destination moves
    /// noticeably. Steering stops once the agent is in range.
    /// </summary>
    public CEGOAPSteeringStatus Navigate(EntityUid uid, EntityCoordinates destination, float range)
    {
        var steering = EnsureComp<CEGOAPSteeringComponent>(uid);

        var restart = steering.Target is not { } current
                      || !MathHelper.CloseTo(steering.TargetRange, range)
                      || !current.TryDistance(EntityManager, destination, out var moved)
                      || moved > steering.ReregisterThreshold;

        if (restart)
        {
            steering.Target = destination;
            steering.TargetRange = range;
            steering.NextFreeAttempt = TimeSpan.Zero;
        }

        return Advance((uid, steering), destination, restart);
    }

    /// <summary>
    /// Follows <paramref name="target"/> until within <paramref name="range"/>. Steers to the entity itself
    /// rather than to a snapshot of its position, so a moving target only re-paths when it drifts away
    /// from the end of the current path instead of restarting steering.
    /// </summary>
    public CEGOAPSteeringStatus Navigate(EntityUid uid, EntityUid target, float range)
    {
        return Navigate(uid, new EntityCoordinates(target, Vector2.Zero), range);
    }

    /// <summary>
    /// Keeps moving towards the destination given to the last <see cref="Navigate"/> call.
    /// Returns <see cref="CEGOAPSteeringStatus.InRange"/> when there is nowhere to go.
    /// </summary>
    public CEGOAPSteeringStatus Continue(EntityUid uid)
    {
        if (!TryComp<CEGOAPSteeringComponent>(uid, out var steering) || steering.Target is not { } target)
            return CEGOAPSteeringStatus.InRange;

        return Advance((uid, steering), target, false);
    }

    /// <summary>
    /// Stops moving and forgets the destination.
    /// </summary>
    public void Stop(Entity<CEGOAPSteeringComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Target = null;
        ent.Comp.PendingAscent = null;
        ent.Comp.PendingDescent = null;
        Halt((ent, ent.Comp));
    }

    #endregion

    private CEGOAPSteeringStatus Advance(Entity<CEGOAPSteeringComponent> ent, EntityCoordinates destination, bool restart)
    {
        var (uid, steering) = ent;

        if (!_xformQuery.TryGetComponent(uid, out var xform))
            return CEGOAPSteeringStatus.NoPath;

        // Free the agent first; while it is held, steering would only report it can't move.
        if (_timing.CurTime >= steering.NextFreeAttempt)
        {
            steering.NextFreeAttempt = _timing.CurTime + steering.FreeRetryInterval;
            TryFree(ent);
        }

        if (IsHeld(uid))
        {
            Halt(ent);
            return CEGOAPSteeringStatus.Moving;
        }

        var sameMap = xform.MapUid == _transform.GetMap(destination);
        if (sameMap && xform.Coordinates.TryDistance(EntityManager, destination, out var distance) && distance <= steering.TargetRange)
        {
            Stop((uid, steering));
            return CEGOAPSteeringStatus.InRange;
        }

        // Steering arrived at a destination that has since moved slightly: walk the rest.
        var arrivedEarly = sameMap && steering.Steering && steering.Status == CEGOAPSteeringStatus.InRange;

        if (restart || !steering.Steering || arrivedEarly)
        {
            BeginSteering(ent, destination);
            return steering.Status;
        }

        switch (steering.Status)
        {
            case CEGOAPSteeringStatus.NoPath:
                return CEGOAPSteeringStatus.NoPath;
            case CEGOAPSteeringStatus.InRange when !sameMap:
                CrossZLevel(ent);
                break;
        }

        return CEGOAPSteeringStatus.Moving;
    }

    #region Freeing

    /// <summary>
    /// One attempt to get rid of whatever keeps the agent from moving.
    /// </summary>
    private void TryFree(Entity<CEGOAPSteeringComponent> ent)
    {
        var (uid, steering) = ent;

        if (steering.Unbuckle && _buckleQuery.TryComp(uid, out var buckle) && buckle.Buckled)
            _buckle.TryUnbuckle(uid, uid, popup: false);

        if (steering.Unpull && _pullableQuery.TryComp(uid, out var pullable) && pullable.BeingPulled &&
            _actionBlocker.CanInteract(uid, uid))
            _pulling.TryStopPull(uid, pullable, uid);

        if (steering.EscapeStorage && _container.TryGetContainingContainer(uid, out var container) &&
            !_entityStorage.TryOpenStorage(uid, container.Owner))
            TryAttack(uid, container.Owner);
    }

    private bool IsHeld(EntityUid uid)
    {
        return _buckleQuery.TryComp(uid, out var buckle) && buckle.Buckled || _container.IsEntityInContainer(uid);
    }

    /// <summary>
    /// Hits <paramref name="target"/> with the agent's weapon. Returns false if the agent can't attack right now.
    /// </summary>
    private bool TryAttack(EntityUid uid, EntityUid target)
    {
        if (!_weapon.TryGetWeapon(uid, out var weapon) || !TryComp<CombatModeComponent>(uid, out var combatMode))
            return false;

        var direction = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(uid);
        var angle = direction == Vector2.Zero ? Angle.Zero : Angle.FromWorldVec(direction);

        _combat.SetInCombatMode(uid, true, combatMode);
        var attacked = _weapon.TryUse(uid, weapon.Value, CEUseType.Primary, angle);
        _combat.SetInCombatMode(uid, false, combatMode);
        return attacked;
    }

    #endregion

    #region Z-levels

    /// <summary>
    /// Starts steering to the destination, or to the nearest slope if the destination is on another Z-level.
    /// </summary>
    private void BeginSteering(Entity<CEGOAPSteeringComponent> ent, EntityCoordinates destination)
    {
        var (uid, steering) = ent;
        steering.PendingAscent = null;
        steering.PendingDescent = null;

        if (!_xformQuery.TryGetComponent(uid, out var xform) || xform.MapUid is not { } agentMap ||
            _transform.GetMap(destination) is not { } destinationMap)
        {
            SetNoPath(ent);
            return;
        }

        if (agentMap == destinationMap)
        {
            SteerTo(ent, destination, steering.TargetRange);
            return;
        }

        var zOffset = GetZOffset(agentMap, destinationMap);
        if (zOffset == 0 || !_gridQuery.TryGetComponent(agentMap, out var grid))
        {
            SetNoPath(ent);
            return;
        }

        var agentWorldPos = _transform.GetWorldPosition(xform);

        if (zOffset > 0)
        {
            // Steer to the uphill edge of the nearest slope on the current map.
            if (!_ladderCache.GetNearestLadder(agentMap, agentWorldPos, 5, out var slopeTilePos, out var slope))
            {
                SetNoPath(ent);
                return;
            }

            var uphillDir = slope.Direction.GetOpposite();
            var slopeTileCenter = _mapSystem.GridTileToLocal(agentMap, grid, slopeTilePos);
            SteerTo(ent, new EntityCoordinates(slopeTileCenter.EntityId, slopeTileCenter.Position + uphillDir.ToVec() * 0.45f), 0.3f);
            steering.PendingAscent = slope.Direction;
        }
        else
        {
            // Find the nearest slope on the map below and walk to the tile above its uphill end.
            if (!_zLevels.TryMapDown(agentMap, out var belowMap) ||
                !_ladderCache.GetNearestLadder(belowMap, agentWorldPos, 5, out var slopeTilePos, out var slope))
            {
                SetNoPath(ent);
                return;
            }

            var approachTile = slopeTilePos + slope.Direction.GetOpposite().ToIntVec();
            if (!_mapSystem.TryGetTileRef(agentMap, grid, approachTile, out var tileRef) || tileRef.Tile.IsEmpty)
            {
                SetNoPath(ent);
                return;
            }

            var tileCenter = _mapSystem.GridTileToLocal(agentMap, grid, approachTile);
            SteerTo(ent, new EntityCoordinates(tileCenter.EntityId, tileCenter.Position + slope.Direction.ToVec() * 0.4f), 0.3f);
            steering.PendingDescent = (slope.Direction, belowMap.Owner);
        }
    }

    /// <summary>
    /// Reached the slope while the destination is on another Z-level: move to the neighbouring map.
    /// Steering restarts towards the destination on the next poll.
    /// </summary>
    private void CrossZLevel(Entity<CEGOAPSteeringComponent> ent)
    {
        var (uid, steering) = ent;

        if (steering.PendingAscent is { } ascentDir)
        {
            _zLevels.TryMoveUp(uid);
            // ascentDir is downhill; shift uphill to land on the upper map's floor.
            var pos = _transform.GetWorldPosition(uid);
            _transform.SetWorldPosition(uid, pos + ascentDir.GetOpposite().ToVec() * 0.25f);
        }
        else if (steering.PendingDescent is { } descent && _mapQuery.TryComp(descent.BelowMap, out var belowMapComp))
        {
            var pos = _transform.GetWorldPosition(uid);
            _transform.SetMapCoordinates(uid, new MapCoordinates(pos + descent.SlopeDir.ToVec() * 0.75f, belowMapComp.MapId));
        }

        steering.PendingAscent = null;
        steering.PendingDescent = null;
        Halt(ent);
    }

    /// <summary>
    /// Z-offset from the agent's map to the destination map: positive if above, negative if below,
    /// 0 if the maps aren't in the same Z-network.
    /// </summary>
    private int GetZOffset(EntityUid agentMap, EntityUid destinationMap)
    {
        if (!_zMapQuery.TryGetComponent(agentMap, out var agentZMap) ||
            !_zMapQuery.TryGetComponent(destinationMap, out var destinationZMap))
            return 0;

        if (!_zLevels.TryGetMapNetwork(agentMap, out var agentNetwork) ||
            !_zLevels.TryGetMapNetwork(destinationMap, out var destinationNetwork) ||
            agentNetwork.Owner != destinationNetwork.Owner)
            return 0;

        return destinationZMap.Depth - agentZMap.Depth;
    }

    #endregion

    #region Steering state

    private void SteerTo(Entity<CEGOAPSteeringComponent> ent, EntityCoordinates coordinates, float range)
    {
        var steering = ent.Comp;
        CancelPath(steering);
        steering.Coordinates = coordinates;
        steering.Range = range;
        steering.Steering = true;
        steering.Status = CEGOAPSteeringStatus.Moving;
        steering.FailedPathCount = 0;
        ResetStuck(steering, Transform(ent).Coordinates);
    }

    private void SetNoPath(Entity<CEGOAPSteeringComponent> ent)
    {
        Halt(ent);
        ent.Comp.Status = CEGOAPSteeringStatus.NoPath;
    }

    /// <summary>
    /// Stops steering and movement input but keeps the destination.
    /// </summary>
    private void Halt(Entity<CEGOAPSteeringComponent> ent)
    {
        var (uid, steering) = ent;
        CancelPath(steering);
        Array.Clear(steering.Interest);
        Array.Clear(steering.Danger);

        if (!steering.Steering)
            return;

        steering.Steering = false;

        if (TryComp<InputMoverComponent>(uid, out var mover))
        {
            mover.CurTickSprintMovement = Vector2.Zero;

            var ev = new SpriteMoveEvent(false);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private static void CancelPath(CEGOAPSteeringComponent steering)
    {
        steering.PathfindToken?.Cancel();
        steering.PathfindToken = null;
        steering.CurrentPath.Clear();
    }

    /// <summary>
    /// Whether the agent may climb: allowed by its settings and physically able to.
    /// </summary>
    private bool CanClimb(EntityUid uid, CEGOAPSteeringComponent steering)
    {
        return steering.Climb && HasComp<ClimbingComponent>(uid);
    }

    private PathFlags GetPathFlags(EntityUid uid, CEGOAPSteeringComponent steering)
    {
        var flags = PathFlags.None;
        // Routing over climbables the agent can't climb would just walk it into the obstacle.
        if (CanClimb(uid, steering))
            flags |= PathFlags.Climbing;
        if (steering.Smash)
            flags |= PathFlags.Smashing;
        if (steering.Pry)
            flags |= PathFlags.Prying;
        if (steering.Interact)
            flags |= PathFlags.Interact;
        return flags;
    }

    #endregion

    private void SetEnabled(bool value)
    {
        _enabled = value;
        if (value)
            return;

        var query = EntityQueryEnumerator<CEGOAPSteeringComponent>();
        while (query.MoveNext(out var uid, out var steering))
        {
            Halt((uid, steering));
        }
    }

    private void OnDebugRequest(RequestNPCSteeringDebugEvent msg, EntitySessionEventArgs args)
    {
        if (!_admin.IsAdmin(args.SenderSession))
            return;

        if (msg.Enabled)
            _subscribedSessions.Add(args.SenderSession);
        else
            _subscribedSessions.Remove(args.SenderSession);
    }

    private void OnSteeringShutdown(Entity<CEGOAPSteeringComponent> ent, ref ComponentShutdown args)
    {
        // Cancel any active pathfinding jobs as they're irrelevant.
        CancelPath(ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled)
            return;

        _agents.Clear();
        var query = EntityQueryEnumerator<CEActiveGOAPComponent, CEGOAPSteeringComponent, InputMoverComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var steering, out var mover, out var xform))
        {
            if (steering.Steering)
                _agents.Add((uid, steering, mover, xform));
        }

        var curTime = _timing.CurTime;
        foreach (var (uid, steering, mover, xform) in _agents)
        {
            Steer(uid, steering, mover, xform, frameTime, curTime);
        }

        if (_subscribedSessions.Count == 0)
            return;

        var data = new List<NPCSteeringDebugData>(_agents.Count);
        foreach (var (uid, steering, mover, _) in _agents)
        {
            data.Add(new NPCSteeringDebugData(
                GetNetEntity(uid),
                mover.CurTickSprintMovement,
                steering.Interest,
                steering.Danger,
                steering.DangerPoints));
        }

        var filter = Filter.Empty();
        filter.AddPlayers(_subscribedSessions);
        RaiseNetworkEvent(new NPCSteeringDebugEvent(data), filter);
    }

    private void SetDirection(EntityUid uid, InputMoverComponent component, CEGOAPSteeringComponent steering, Vector2 value, bool clear = true)
    {
        if (clear && value.Equals(Vector2.Zero))
        {
            steering.CurrentPath.Clear();
            Array.Clear(steering.Interest);
            Array.Clear(steering.Danger);
        }

        component.CurTickSprintMovement = value;
        component.LastInputTick = _timing.CurTick;
        component.LastInputSubTick = ushort.MaxValue;

        var ev = new SpriteMoveEvent(true);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Go through each steerer and combine their vectors
    /// </summary>
    private void Steer(
        EntityUid uid,
        CEGOAPSteeringComponent steering,
        InputMoverComponent mover,
        TransformComponent xform,
        float frameTime,
        TimeSpan curTime)
    {
        if (Deleted(steering.Coordinates.EntityId))
        {
            SetDirection(uid, mover, steering, Vector2.Zero);
            steering.Status = CEGOAPSteeringStatus.NoPath;
            return;
        }

        // No path set from pathfinding or the likes.
        if (steering.Status == CEGOAPSteeringStatus.NoPath)
        {
            SetDirection(uid, mover, steering, Vector2.Zero);
            return;
        }

        // Can't move at all, just noop input.
        if (!mover.CanMove)
        {
            SetDirection(uid, mover, steering, Vector2.Zero);
            steering.Status = CEGOAPSteeringStatus.NoPath;
            return;
        }

        var agentRadius = steering.Radius;
        var worldPos = _transform.GetWorldPosition(xform);
        var (layer, mask) = _physics.GetHardCollision(uid);

        // Use rotation relative to parent to rotate our context vectors by.
        var offsetRot = -_mover.GetParentGridAngle(mover);
        _modifierQuery.TryGetComponent(uid, out var modifier);
        var moveSpeed = GetSprintSpeed(uid, modifier);
        var body = _physicsQuery.GetComponent(uid);
        steering.DangerPoints.Clear();
        Span<float> interest = stackalloc float[InterestDirections];
        Span<float> danger = stackalloc float[InterestDirections];

        steering.CanSeek = true;

        // If seek has arrived at the target node for example then immediately re-steer.
        var forceSteer = true;

        if (steering.CanSeek && !TrySeek(uid, mover, steering, body, xform, offsetRot, moveSpeed, interest, frameTime, ref forceSteer))
        {
            SetDirection(uid, mover, steering, Vector2.Zero);
            return;
        }

        DebugTools.Assert(!float.IsNaN(interest[0]));

        // Don't steer too frequently to avoid twitchiness.
        if (!forceSteer)
        {
            SetDirection(uid, mover, steering, steering.LastSteerDirection, false);
            return;
        }

        // Avoid static objects like walls, except the obstacle on the next path node: the agent has to touch it
        // to climb or smash it, and avoidance would keep it just out of reach.
        _ignoredObstacles.Clear();
        if (steering.CurrentPath.TryPeek(out var nextNode) && !nextNode.Data.IsFreeSpace)
            GetObstacleEntities(nextNode, mask, layer, _ignoredObstacles);

        CollisionAvoidance(uid, offsetRot, worldPos, agentRadius, layer, mask, xform, danger, _ignoredObstacles);
        DebugTools.Assert(!float.IsNaN(danger[0]));

        Separation(uid, offsetRot, worldPos, agentRadius, layer, mask, body, xform, danger);

        // Blend last and current tick
        Blend(steering, frameTime, interest, danger);

        // Remove the danger map from the interest map.
        var desiredDirection = -1;
        var desiredValue = 0f;

        for (var i = 0; i < InterestDirections; i++)
        {
            var adjustedValue = Math.Clamp(steering.Interest[i] - steering.Danger[i], 0f, 1f);

            if (adjustedValue > desiredValue)
            {
                desiredDirection = i;
                desiredValue = adjustedValue;
            }
        }

        var resultDirection = Vector2.Zero;

        if (desiredDirection != -1)
        {
            resultDirection = new Angle(desiredDirection * InterestRadians).ToVec();
        }

        steering.LastSteerDirection = resultDirection;
        DebugTools.Assert(!float.IsNaN(resultDirection.X));
        SetDirection(uid, mover, steering, resultDirection, false);
    }

    private EntityCoordinates GetCoordinates(PathPoly poly)
    {
        if (!poly.IsValid())
            return EntityCoordinates.Invalid;

        return new EntityCoordinates(poly.GraphUid, poly.Box.Center);
    }

    /// <summary>
    /// Get a new job from the pathfindingsystem
    /// </summary>
    private async void RequestPath(EntityUid uid, CEGOAPSteeringComponent steering, TransformComponent xform, float targetDistance)
    {
        // If we already have a pathfinding request then don't grab another.
        if (steering.Pathfind)
            return;

        // If we're in range then just beeline them; this can avoid stutter stepping and is an easy way to look nicer.
        // CrystallEdge: only with a clear line. Obstacles (climb, smash, pry) are handled on path nodes, so
        // beelining into a fence next to the target would just rub against it.
        if (targetDistance < steering.RepathRange &&
            _interaction.InRangeUnobstructed(uid, steering.Coordinates, steering.RepathRange + 0.5f))
            return;

        // Short-circuit with no path.
        var targetPoly = _pathfindingSystem.GetPoly(steering.Coordinates);

        if (targetPoly != null &&
            steering.Coordinates.Position.Equals(Vector2.Zero) &&
            TryComp<PhysicsComponent>(uid, out var physics) &&
            _interaction.InRangeUnobstructed(uid, steering.Coordinates.EntityId, range: 30f, (CollisionGroup) physics.CollisionMask))
        {
            steering.CurrentPath.Clear();
            steering.CurrentPath.Enqueue(targetPoly);
            return;
        }

        steering.PathfindToken = new CancellationTokenSource();

        var result = await _pathfindingSystem.GetPathSafe(
            uid,
            xform.Coordinates,
            steering.Coordinates,
            steering.Range,
            steering.PathfindToken.Token,
            GetPathFlags(uid, steering));

        steering.PathfindToken = null;

        if (result.Result == PathResult.NoPath)
        {
            steering.CurrentPath.Clear();
            steering.FailedPathCount++;

            if (steering.FailedPathCount >= CEGOAPSteeringComponent.FailedPathLimit)
            {
                steering.Status = CEGOAPSteeringStatus.NoPath;
            }

            return;
        }

        var targetPos = _transform.ToMapCoordinates(steering.Coordinates);
        var ourPos = _transform.GetMapCoordinates(uid, xform: xform);

        PrunePath(uid, ourPos, targetPos.Position - ourPos.Position, result.Path);
        steering.CurrentPath = new Queue<PathPoly>(result.Path);
    }

    private float GetSprintSpeed(EntityUid uid, MovementSpeedModifierComponent? modifier = null)
    {
        if (!Resolve(uid, ref modifier, false))
        {
            return MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
        }

        return modifier.CurrentSprintSpeed;
    }
}
