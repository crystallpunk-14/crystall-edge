using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Robust.Shared.Random;

namespace Content.Server._CE.GOAP.Selectors;

/// <summary>
/// Picks a random entity out of everything the agent currently knows
/// (<see cref="CEGOAPComponent.Knowledge"/>) that passes the selector's
/// <see cref="CEGOAPTargetSelector.Conditions"/>.
/// </summary>
public sealed partial class CEGOAPSelectorRandomKnown : CEGOAPTargetSelectorBase<CEGOAPSelectorRandomKnown>
{
}

public sealed partial class CEGOAPSelectorRandomKnownSystem : CEGOAPTargetSelectorSystem<CEGOAPSelectorRandomKnown>
{
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<CEGOAPComponent> _goapQuery = default!;

    private readonly List<EntityUid> _candidates = new();

    protected override void Resolve(ref CEGOAPSelectorResolveEvent<CEGOAPSelectorRandomKnown> ev)
    {
        if (!_goapQuery.TryComp(ev.Agent, out var goap))
            return;

        _candidates.Clear();
        foreach (var known in goap.Knowledge.Keys)
        {
            if (known == ev.Agent || TerminatingOrDeleted(known))
                continue;

            if (ev.Selector.CandidatePasses(ev.Agent, known, EntityManager))
                _candidates.Add(known);
        }

        if (_candidates.Count == 0)
            return;

        var chosen = _random.Pick(_candidates);
        _candidates.Clear();

        ev.Entity = chosen;
        if (_xformQuery.TryComp(chosen, out var xform))
            ev.Position = xform.Coordinates;
    }
}
