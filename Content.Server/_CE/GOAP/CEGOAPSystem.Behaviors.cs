using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GOAP;

/// <summary>
/// Partial: expands <see cref="CEGOAPComponent.Behaviors"/> packages onto the agent on MapInit.
/// </summary>
public sealed partial class CEGOAPSystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly HashSet<ProtoId<CEGOAPBehaviorPrototype>> _appliedBehaviors = new();

    /// <summary>
    /// Appends target slots, goals and actions of every listed package (and its includes) to the agent and
    /// attaches the package sensors. Each package is applied once, even if reached through several includes.
    /// </summary>
    private void MergeBehaviors(Entity<CEGOAPComponent> ent)
    {
        if (ent.Comp.Behaviors.Count == 0)
            return;

        _appliedBehaviors.Clear();
        foreach (var behavior in ent.Comp.Behaviors)
        {
            ApplyBehavior(ent, behavior);
        }
    }

    private void ApplyBehavior(Entity<CEGOAPComponent> ent, ProtoId<CEGOAPBehaviorPrototype> id)
    {
        if (!_appliedBehaviors.Add(id))
            return;

        var behavior = _proto.Index(id);
        foreach (var include in behavior.Includes)
        {
            ApplyBehavior(ent, include);
        }

        foreach (var (slot, selector) in behavior.Targets)
        {
            if (!ent.Comp.Targets.TryAdd(slot, selector))
                Log.Error($"GOAP target slot {slot} on {ToPrettyString(ent)} is defined more than once (again by behavior {id})");
        }

        ent.Comp.Goals.AddRange(behavior.Goals);
        ent.Comp.Actions.AddRange(behavior.Actions);

        foreach (var sensor in behavior.Sensors)
        {
            sensor.AddTo(ent, EntityManager);
        }
    }
}
