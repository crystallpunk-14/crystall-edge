using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPHasStatusEffectSensorEntry
{
    [DataField(required: true)]
    public string ConditionKey = string.Empty;

    [DataField(required: true)]
    public CEGOAPTargetSelector Selector = default!;

    /// <summary>
    /// Prototype ID of the status effect entity to check for.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId StatusEffect;
}

/// <summary>
/// Checks if the entity has a specific status effect active.
/// Event-driven: reacts to status effect applied/removed events on this entity.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPHasStatusEffectSensorComponent : Component
{
    [DataField]
    [AlwaysPushInheritance]
    public List<CEGOAPHasStatusEffectSensorEntry> Entries = [];
}

public sealed partial class CEGOAPHasStatusEffectSensorSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffect = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGOAPHasStatusEffectSensorComponent, CEGOAPSensorRefreshEvent>(OnRefresh);

        // CrystallEdge: Rogue's CE status effect system raised these directly on the affected
        // entity. Vanilla's StatusEffectNew system raises them on the effect entity itself
        // (see StatusEffectAppliedEvent/StatusEffectRemovedEvent docs), carrying the affected
        // entity in the Target field — so we subscribe broadcast and re-evaluate the target.
        SubscribeLocalEvent<StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<StatusEffectRemovedEvent>(OnEffectRemoved);
    }

    private void OnRefresh(Entity<CEGOAPHasStatusEffectSensorComponent> ent, ref CEGOAPSensorRefreshEvent args)
    {
        EvaluateAll(ent);
    }

    private void OnEffectApplied(ref StatusEffectAppliedEvent args)
    {
        if (TryComp<CEGOAPHasStatusEffectSensorComponent>(args.Target, out var sensor))
            EvaluateAll((args.Target, sensor));
    }

    private void OnEffectRemoved(ref StatusEffectRemovedEvent args)
    {
        if (TryComp<CEGOAPHasStatusEffectSensorComponent>(args.Target, out var sensor))
            EvaluateAll((args.Target, sensor));
    }

    private void EvaluateAll(Entity<CEGOAPHasStatusEffectSensorComponent> ent)
    {
        if (!TryComp<CEGOAPComponent>(ent, out var goap))
            return;

        foreach (var entry in ent.Comp.Entries)
        {
            EvaluateEntry(ent, entry, goap);
        }
    }

    private void EvaluateEntry(EntityUid uid, CEGOAPHasStatusEffectSensorEntry entry, CEGOAPComponent goap)
    {
        var result = entry.Selector.Resolve(uid, EntityManager);
        if (result.Entity is not { } target)
        {
            goap.WorldState[entry.ConditionKey] = false;
            return;
        }

        goap.WorldState[entry.ConditionKey] = _statusEffect.HasStatusEffect(target, entry.StatusEffect);
    }
}
