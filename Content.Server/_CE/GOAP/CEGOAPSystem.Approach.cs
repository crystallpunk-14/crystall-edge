using Content.Server._CE.GOAP.Steering;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Robust.Shared.Map;

namespace Content.Server._CE.GOAP;

/// <summary>
/// Partial: actions performed at a distance from their target (<see cref="CEGOAPAction.Range"/>).
/// The orchestrator walks the agent into range itself, so movement is never a step of the plan,
/// and prices the walk into the action cost.
/// </summary>
public sealed partial class CEGOAPSystem
{
    [Dependency] private CEGOAPSteeringSystem _steering = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    /// <summary>
    /// Cost of <paramref name="action"/> for the next plan: its base cost plus the walk to its target.
    /// Returns false when the action's target slot resolves to nothing, so the action can't be planned.
    /// </summary>
    private bool TryGetPlanningCost(Entity<CEGOAPComponent> ent, CEGOAPAction action, out float cost)
    {
        cost = action.Cost;

        if (action.Target == null)
            return true;

        if (!TryGetCoordinates(ResolveTarget(ent, action.Target), out var targetCoords))
            return false;

        if (action.Range is { } range
            && _xformQuery.TryComp(ent, out var xform)
            && xform.Coordinates.TryDistance(EntityManager, targetCoords, out var distance))
        {
            cost += MathF.Max(0f, distance - range) * ent.Comp.DistanceCost;
        }

        return true;
    }

    /// <summary>
    /// Moves the agent towards the target of <paramref name="action"/>, which has a <see cref="CEGOAPAction.Range"/>.
    /// Returns <see cref="CEGOAPSteeringStatus.NoPath"/> when the target is gone or unreachable.
    /// </summary>
    private CEGOAPSteeringStatus Approach(EntityUid uid, CEGOAPAction action, float range)
    {
        var target = ResolveTarget(uid, action.Target);

        if (target.Entity is { } entity && _xformQuery.HasComp(entity))
            return _steering.Navigate(uid, entity, range);

        if (target.Position is { } position)
            return _steering.Navigate(uid, position, range);

        return CEGOAPSteeringStatus.NoPath;
    }

    /// <summary>
    /// Position of a resolved target, preferring the entity's current position.
    /// </summary>
    private bool TryGetCoordinates(CEGOAPSelectorResult target, out EntityCoordinates coords)
    {
        if (target.Entity is { } entity && _xformQuery.TryComp(entity, out var xform))
        {
            coords = xform.Coordinates;
            return true;
        }

        if (target.Position is { } position)
        {
            coords = position;
            return true;
        }

        coords = default;
        return false;
    }
}
