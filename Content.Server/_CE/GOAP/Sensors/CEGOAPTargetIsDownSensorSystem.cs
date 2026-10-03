using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Analyzers;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPTargetIsDownSensorEntry
{
    [DataField(required: true)]
    public string ConditionKey = string.Empty;

    [DataField(required: true)]
    public CEGOAPTargetSelector Selector = default!;
}

/// <summary>
/// Checks if a selector-resolved target is incapacitated (critical or dead).
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPTargetIsDownSensorComponent : Component
{
    [DataField]
    [AlwaysPushInheritance]
    public List<CEGOAPTargetIsDownSensorEntry> Entries = [];
}

public sealed partial class CEGOAPTargetIsDownSensorSystem : EntitySystem
{
    // CrystallEdge: Rogue used CEMobStateSystem (CE-only). This fork has no CE health stack,
    // so use vanilla MobStateSystem instead.
    [Dependency] private MobStateSystem _mobState = default!;

    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    [SubscribeLocalEvent]
    private void OnKnowledgeUpdated(Entity<CEGOAPTargetIsDownSensorComponent> ent, ref CEGOAPKnowledgeUpdatedEvent args)
    {
        EvaluateAll(ent);
    }

    [SubscribeLocalEvent]
    private void OnRefresh(Entity<CEGOAPTargetIsDownSensorComponent> ent, ref CEGOAPSensorRefreshEvent args)
    {
        EvaluateAll(ent);
    }

    private void EvaluateAll(Entity<CEGOAPTargetIsDownSensorComponent> ent)
    {
        if (!TryComp<CEGOAPComponent>(ent, out var goap))
            return;

        foreach (var entry in ent.Comp.Entries)
            EvaluateEntry((ent.Owner, goap), entry);
    }

    private void EvaluateEntry(Entity<CEGOAPComponent> ent, CEGOAPTargetIsDownSensorEntry entry)
    {
        var result = entry.Selector.Resolve(ent, EntityManager);
        var isDown = result.Entity is { } target && (
            _mobStateQuery.TryGetComponent(target, out var mobState)
                ? _mobState.IsIncapacitated(target, mobState)
                : Terminating(target));
        ent.Comp.WorldState[entry.ConditionKey] = isDown;
    }
}
