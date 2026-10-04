using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Robust.Shared.Analyzers;
using Content.Shared._CE.GOAP.Sensors;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPHasTargetSensorEntry : CEGOAPSensorEntryBase
{
    public override void AddTo(EntityUid uid, IEntityManager entMan)
    {
        entMan.EnsureComponent<CEGOAPHasTargetSensorComponent>(uid).Entries.Add(this);
    }
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
    [Dependency] private CEGOAPSystem _goap = default!;
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
            var result = _goap.ResolveTarget(ent, entry.Target);
            goap.WorldState[entry.ConditionKey] = result.Entity != null;
        }
    }
}
