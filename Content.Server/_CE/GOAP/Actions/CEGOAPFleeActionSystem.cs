using System.Numerics;
using Content.Server._CE.GOAP.Steering;
using Content.Server.NPC.Pathfinding;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared.NPC;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>
/// Flee away from its current target using BFS over pathfinding polygons.
/// Every recalculation interval, performs a BFS from the NPC's current tile
/// and picks the reachable tile farthest from the threat.
/// </summary>
public sealed partial class CEGOAPFleeAction : CEGOAPActionBase<CEGOAPFleeAction>
{
    /// <summary>
    /// Maximum BFS iterations (depth) when searching for flee destinations.
    /// </summary>
    [DataField]
    public int MaxBfsIterations = 10;

    /// <summary>
    /// How often to recalculate the flee destination, in seconds.
    /// </summary>
    [DataField]
    public float RecalculateInterval = 1f;
}

public sealed partial class CEGOAPFleeActionSystem : CEGOAPActionSystem<CEGOAPFleeAction>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private CEGOAPSteeringSystem _steering = default!;
    [Dependency] private PathfindingSystem _pathfinding = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _nextRecalc = new();

    protected override void OnActionStartup(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionStartupEvent<CEGOAPFleeAction> args)
    {
        var result = Goap.ResolveTarget(ent, args.Action.Target);
        if (result.Entity is not { } target)
            return;

        FindAndRegisterFleeTarget(ent, target, args.Action);
        _nextRecalc[ent] = _timing.CurTime + TimeSpan.FromSeconds(args.Action.RecalculateInterval);
    }

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPFleeAction> args)
    {
        var result = Goap.ResolveTarget(ent, args.Action.Target);
        if (result.Entity is not { } target)
        {
            args.Status = CEGOAPActionStatus.Finished;
            return;
        }

        // Recalculate flee destination periodically.
        if (_timing.CurTime >= _nextRecalc.GetValueOrDefault(ent))
        {
            FindAndRegisterFleeTarget(ent, target, args.Action);
            _nextRecalc[ent] = _timing.CurTime + TimeSpan.FromSeconds(args.Action.RecalculateInterval);
        }

        switch (_steering.Continue(ent))
        {
            case CEGOAPSteeringStatus.InRange:
                // Reached the flee point — keep running, recalc will pick a new one.
                args.Status = CEGOAPActionStatus.Running;
                return;
            case CEGOAPSteeringStatus.NoPath:
                args.Status = CEGOAPActionStatus.Failed;
                return;
            default:
                args.Status = CEGOAPActionStatus.Running;
                return;
        }
    }

    protected override void OnActionShutdown(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionShutdownEvent<CEGOAPFleeAction> args)
    {
        _nextRecalc.Remove(ent);
        _steering.Stop(ent.Owner);
    }

    /// <summary>
    /// BFS over PathPoly neighbors from the NPC's current position.
    /// Picks the reachable tile farthest from the threat within MaxBfsIterations depth.
    /// </summary>
    private void FindAndRegisterFleeTarget(
        Entity<CEGOAPComponent> ent,
        EntityUid threat,
        CEGOAPFleeAction action)
    {
        var npcCoords = Transform(ent).Coordinates;
        var startPoly = _pathfinding.GetPoly(npcCoords);
        if (startPoly == null)
            return;

        var threatWorldPos = _transform.GetWorldPosition(Transform(threat));

        // BFS: explore neighbors up to MaxBfsIterations depth.
        var visited = new HashSet<PathPoly> { startPoly };
        var frontier = new List<PathPoly> { startPoly };

        PathPoly? bestPoly = null;
        var bestDistSq = float.MinValue;

        for (var depth = 0; depth < action.MaxBfsIterations && frontier.Count > 0; depth++)
        {
            var nextFrontier = new List<PathPoly>();

            foreach (var poly in frontier)
            {
                foreach (var neighbor in poly.Neighbors)
                {
                    if (!visited.Add(neighbor))
                        continue;

                    if (!neighbor.IsValid())
                        continue;

                    // Never flee into space tiles.
                    if ((neighbor.Data.Flags & PathfindingBreadcrumbFlag.Space) != 0)
                        continue;

                    nextFrontier.Add(neighbor);

                    // Score by squared distance from threat (avoid sqrt).
                    var polyWorldPos = _transform.ToMapCoordinates(neighbor.Coordinates).Position;
                    var distSq = Vector2.DistanceSquared(polyWorldPos, threatWorldPos);
                    if (distSq > bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestPoly = neighbor;
                    }
                }
            }

            frontier = nextFrontier;
        }

        if (bestPoly == null)
            return;

        _steering.Navigate(ent, bestPoly.Coordinates, 1.5f);
    }
}
