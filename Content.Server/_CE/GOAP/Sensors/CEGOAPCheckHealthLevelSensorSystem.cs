using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPCheckHealthLevelSensorEntry
{
    [DataField(required: true)]
    public string ConditionKey = string.Empty;

    [DataField(required: true)]
    public CEGOAPTargetSelector Selector = default!;

    /// <summary>
    /// Health fraction (0..1) below which the condition is set to true.
    /// </summary>
    [DataField]
    public float Threshold = 0.5f;
}

/// <summary>
/// Checks if the entity's own health fraction is below a threshold.
/// Event-driven via DamageDealtEvent.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPCheckHealthLevelSensorComponent : Component
{
    [DataField]
    [AlwaysPushInheritance]
    public List<CEGOAPCheckHealthLevelSensorEntry> Entries = [];
}

public sealed partial class CEGOAPCheckHealthLevelSensorSystem : EntitySystem
{
    // CrystallEdge: Rogue used CESharedDamageableSystem.GetHealthInfo() (CE-only). This fork
    // has no CE health stack, so compute health fraction from vanilla MobThresholdSystem's
    // incapacitation percentage instead.
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGOAPCheckHealthLevelSensorComponent, CEGOAPSensorRefreshEvent>(OnRefresh);
        // Runs after DamageableSystem applies the DamageDealtEvent to DamageableComponent.TotalDamage,
        // so the percentage read below reflects the post-hit value.
        SubscribeLocalEvent<CEGOAPCheckHealthLevelSensorComponent, DamageDealtEvent>(OnDamageDealt,
            after: new[] { typeof(DamageableSystem) });
    }

    private void OnRefresh(Entity<CEGOAPCheckHealthLevelSensorComponent> ent, ref CEGOAPSensorRefreshEvent args)
    {
        EvaluateAll(ent);
    }

    private void OnDamageDealt(Entity<CEGOAPCheckHealthLevelSensorComponent> ent, ref DamageDealtEvent args)
    {
        EvaluateAll(ent);
    }

    private void EvaluateAll(Entity<CEGOAPCheckHealthLevelSensorComponent> ent)
    {
        if (!TryComp<CEGOAPComponent>(ent, out var goap))
            return;

        foreach (var entry in ent.Comp.Entries)
        {
            EvaluateEntry(ent, entry, goap);
        }
    }

    private void EvaluateEntry(EntityUid uid, CEGOAPCheckHealthLevelSensorEntry entry, CEGOAPComponent goap)
    {
        var result = entry.Selector.Resolve(uid, EntityManager);
        if (result.Entity is not { } target)
        {
            goap.WorldState[entry.ConditionKey] = false;
            return;
        }

        var totalDamage = _damageable.GetTotalDamage(target);
        if (!_mobThreshold.TryGetIncapPercentage(target, totalDamage, out var percentage))
        {
            goap.WorldState[entry.ConditionKey] = false;
            return;
        }

        goap.WorldState[entry.ConditionKey] = (float) percentage.Value < entry.Threshold;
    }
}
