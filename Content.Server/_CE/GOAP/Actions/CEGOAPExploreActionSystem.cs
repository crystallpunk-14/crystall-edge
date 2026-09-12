using System.Numerics;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>
/// Picks a random walkable tile within radius and walks to it.
/// Used as a low-priority idle behavior so mobs circulate around their area.
/// </summary>
public sealed partial class CEGOAPExploreAction : CEGOAPActionBase<CEGOAPExploreAction>
{
    /// <summary>
    /// Maximum distance to pick a destination (in tiles).
    /// </summary>
    [DataField]
    public float ExploreRadius = 8f;

    /// <summary>
    /// Number of random directions to sample when looking for a valid destination.
    /// </summary>
    [DataField]
    public int SampleDirections = 12;

    /// <summary>
    /// Minimum idle time at the destination before the action completes.
    /// </summary>
    [DataField]
    public float MinIdleTime = 1f;

    /// <summary>
    /// Maximum idle time at the destination before the action completes.
    /// </summary>
    [DataField]
    public float MaxIdleTime = 2f;
}

public sealed partial class CEGOAPExploreActionSystem : CEGOAPActionSystem<CEGOAPExploreAction>
{
    [Dependency] private NPCSteeringSystem _steering = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _idleUntil = new();

    protected override void OnActionStartup(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionStartupEvent<CEGOAPExploreAction> args)
    {
        if (!_xformQuery.TryGetComponent(ent, out var xform))
            return;

        var destination = PickDestination(xform, args.Action);
        if (destination == null)
            return;

        // Convert world position to local coordinates for steering
        var invMatrix = _transform.GetInvWorldMatrix(xform.ParentUid);
        var localPos = Vector2.Transform(destination.Value, invMatrix);
        var coords = new EntityCoordinates(xform.ParentUid, localPos);

        var comp = _steering.Register(ent, coords);
        comp.Range = 1.5f;
    }

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPExploreAction> args)
    {
        // Once idle timer is set, run it to completion regardless of steering status.
        // InRange fires for only one frame — checking it again would miss the timer.
        if (_idleUntil.TryGetValue(ent, out var idleEnd))
        {
            args.Status = _timing.CurTime >= idleEnd
                ? CEGOAPActionStatus.Finished
                : CEGOAPActionStatus.Running;
            return;
        }

        if (!TryComp<NPCSteeringComponent>(ent, out var steering))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        switch (steering.Status)
        {
            case SteeringStatus.InRange:
                var idleSecs = _random.NextFloat(args.Action.MinIdleTime, args.Action.MaxIdleTime);
                _idleUntil[ent] = _timing.CurTime + TimeSpan.FromSeconds(idleSecs);
                args.Status = CEGOAPActionStatus.Running;
                return;
            case SteeringStatus.NoPath:
                args.Status = CEGOAPActionStatus.Failed;
                return;
            default:
                args.Status = CEGOAPActionStatus.Running;
                return;
        }
    }

    protected override void OnActionShutdown(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionShutdownEvent<CEGOAPExploreAction> args)
    {
        _idleUntil.Remove(ent);
        _steering.Unregister(ent);
    }

    /// <summary>
    /// Samples random directions at progressively shorter distances
    /// and returns the world position of a valid walkable tile.
    /// Fallback to shorter distances ensures mobs can navigate tight corridors.
    /// </summary>
    private Vector2? PickDestination(TransformComponent xform, CEGOAPExploreAction action)
    {
        var worldPos = _transform.GetWorldPosition(xform);
        var mapId = xform.MapID;

        // Try progressively shorter distances to handle corridors.
        float[] distances =
        [
            action.ExploreRadius,
            action.ExploreRadius * 0.65f,
            action.ExploreRadius * 0.4f,
            action.ExploreRadius * 0.2f,
            2f
        ];
        var baseAngle = (float) _random.NextAngle().Theta;

        foreach (var dist in distances)
        {
            if (dist < 1f)
                continue;

            var angleStep = MathF.PI * 2f / action.SampleDirections;
            for (var i = 0; i < action.SampleDirections; i++)
            {
                var angle = baseAngle + angleStep * i;
                var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                var candidatePos = worldPos + dir * dist;

                var mapCoords = new MapCoordinates(candidatePos, mapId);
                if (!_mapSystem.TryFindGridAt(mapCoords, out var gridUid, out var grid))
                    continue;

                var tileIndices = _mapSystem.WorldToTile(gridUid, grid, candidatePos);
                if (!_mapSystem.TryGetTileRef(gridUid, grid, tileIndices, out var tileRef) ||
                    tileRef.Tile.IsEmpty)
                    continue;

                // Skip tiles with anchored entities (walls, furniture).
                if (_mapSystem.AnchoredEntityCount(gridUid, grid, tileIndices) > 0)
                    continue;

                return candidatePos;
            }
        }

        return null;
    }
}
