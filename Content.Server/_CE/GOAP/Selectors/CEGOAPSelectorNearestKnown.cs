using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;

namespace Content.Server._CE.GOAP.Selectors;

/// <summary>
/// Picks the nearest entity out of everything the agent currently knows
/// (<see cref="CEGOAPComponent.Knowledge"/>, filled by perceptors) that passes the selector's
/// <see cref="CEGOAPTargetSelector.Conditions"/>. Not limited to enemies or allies, so what the agent
/// can pick is still only what it has perceived.
/// </summary>
public sealed partial class CEGOAPSelectorNearestKnown : CEGOAPTargetSelectorBase<CEGOAPSelectorNearestKnown>
{
}

public sealed partial class CEGOAPSelectorNearestKnownSystem : CEGOAPTargetSelectorSystem<CEGOAPSelectorNearestKnown>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<CEGOAPComponent> _goapQuery = default!;

    protected override void Resolve(ref CEGOAPSelectorResolveEvent<CEGOAPSelectorNearestKnown> ev)
    {
        if (!_goapQuery.TryComp(ev.Agent, out var goap) || !_xformQuery.TryComp(ev.Agent, out var selfXform))
            return;

        var selfPos = _transform.GetWorldPosition(selfXform);
        EntityUid? best = null;
        var bestDist = float.MaxValue;

        foreach (var known in goap.Knowledge.Keys)
        {
            if (known == ev.Agent || TerminatingOrDeleted(known))
                continue;

            if (!_xformQuery.TryComp(known, out var knownXform))
                continue;

            if (!ev.Selector.CandidatePasses(ev.Agent, known, EntityManager))
                continue;

            var dist = (_transform.GetWorldPosition(knownXform) - selfPos).LengthSquared();
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            best = known;
        }

        if (best is not { } chosen)
            return;

        ev.Entity = chosen;
        ev.Position = Transform(chosen).Coordinates;
    }
}
