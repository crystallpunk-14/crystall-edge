using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Robust.Shared.Analyzers;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPHasTargetSensorEntry
{
    [DataField(required: true)]
    public string ConditionKey = string.Empty;

    [DataField(required: true)]
    public CEGOAPTargetSelector Selector = default!;
}

/// <summary>
/// Writes true when the entry's selector resolves to an entity. Re-evaluated whenever the
/// agent's knowledge changes.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPHasTargetSensorComponent : Component
{
    [DataField]
    [AlwaysPushInheritance]
    public List<CEGOAPHasTargetSensorEntry> Entries = [];
}

public sealed partial class CEGOAPHasTargetSensorSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnKnowledgeUpdated(Entity<CEGOAPHasTargetSensorComponent> ent, ref CEGOAPKnowledgeUpdatedEvent args)
    {
        EvaluateAll(ent);
    }

    [SubscribeLocalEvent]
    private void OnRefresh(Entity<CEGOAPHasTargetSensorComponent> ent, ref CEGOAPSensorRefreshEvent args)
    {
        EvaluateAll(ent);
    }

    private void EvaluateAll(Entity<CEGOAPHasTargetSensorComponent> ent)
    {
        if (!TryComp<CEGOAPComponent>(ent, out var goap))
            return;

        foreach (var entry in ent.Comp.Entries)
        {
            var result = entry.Selector.Resolve(ent, EntityManager);
            goap.WorldState[entry.ConditionKey] = result.Entity != null;
        }
    }
}
