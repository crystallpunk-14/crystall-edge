using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Prototypes;
using Content.Shared._CE.GOAP.Selectors;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GOAP;

/// <summary>
/// Partial: target slots. Actions and sensors reference a slot id; the slot's selector lives in
/// <see cref="CEGOAPComponent.Targets"/>.
/// </summary>
public sealed partial class CEGOAPSystem
{
    /// <summary>
    /// Resolves the agent's target slot to an entity and/or coordinate.
    /// Returns an empty result when no slot is given or the agent doesn't define it.
    /// </summary>
    public CEGOAPSelectorResult ResolveTarget(EntityUid agent, ProtoId<CEGOAPTargetPrototype>? target)
    {
        if (target is not { } slot || !TryComp<CEGOAPComponent>(agent, out var goap))
            return default;

        if (!goap.Targets.TryGetValue(slot, out var selector))
            return default;

        return selector.Resolve(agent, EntityManager);
    }

    /// <summary>
    /// Logs actions that reference a target slot the agent doesn't define. Such an action would
    /// silently never find its target.
    /// </summary>
    private void ValidateTargets(Entity<CEGOAPComponent> ent)
    {
        foreach (var action in ent.Comp.Actions)
        {
            if (action.Target is { } slot && !ent.Comp.Targets.ContainsKey(slot))
                Log.Error($"GOAP action {action.GetType().Name} on {ToPrettyString(ent)} uses undefined target slot {slot}");
        }
    }
}
