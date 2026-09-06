using Content.Shared._CE.Consumption;
using Content.Shared._CE.GOAP.Selectors;
using Content.Server._CE.GOAP.Consumption;
using Content.Server._CE.GOAP.Navigation;

namespace Content.Server._CE.GOAP.Selectors;

/// <summary>
/// Resolves a usable provider through a prototype-authored consumable-source
/// strategy. Provider-specific eligibility remains owned by that strategy.
/// </summary>
[DataDefinition]
public sealed partial class CEGOAPSelectorConsumableProvider
    : CEGOAPTargetSelectorBase<CEGOAPSelectorConsumableProvider>, ICEGOAPTargetBackoffSelector
{
    [DataField(required: true)]
    public CEConsumableSource Source = default!;

    [DataField(required: true)]
    public float Range;
}

public sealed partial class CEGOAPSelectorConsumableProviderSystem
    : CEGOAPTargetSelectorSystem<CEGOAPSelectorConsumableProvider>
{
    [Dependency] private CEGOAPTargetBackoffSystem _backoff = default!;

    protected override void Resolve(ref CEGOAPSelectorResolveEvent<CEGOAPSelectorConsumableProvider> ev)
    {
        if (ev.Selector.Source == null || !float.IsFinite(ev.Selector.Range) || ev.Selector.Range < 0f)
            return;

        EntityUid provider;
        if (TryComp<CEGOAPConsumeComponent>(ev.Agent, out var active) &&
            ReferenceEquals(active.SourceDefinition, ev.Selector.Source) &&
            active.Provider is { } reservedProvider &&
            Exists(reservedProvider))
        {
            provider = reservedProvider;
        }
        else
        {
            var agent = ev.Agent;
            _backoff.Prune(agent);
            if (!ev.Selector.Source.TryFindProvider(
                    agent,
                    ev.Selector.Range,
                    candidate => !_backoff.IsRejected(agent, candidate),
                    EntityManager,
                    out provider))
            {
                return;
            }
        }

        if (!TryComp(provider, out TransformComponent? transform))
            return;

        ev.Entity = provider;
        ev.Position = transform.Coordinates;
    }
}
