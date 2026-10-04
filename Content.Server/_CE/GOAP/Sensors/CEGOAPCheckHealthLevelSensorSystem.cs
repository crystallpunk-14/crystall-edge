using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPCheckHealthLevelSensorEntry
    : CEGOAPSensorEntry<CEGOAPCheckHealthLevelSensorEntry, CEGOAPCheckHealthLevelSensorComponent>
{
    /// <summary>
    /// Health fraction (0..1) below which the condition is set to true.
    /// </summary>
    [DataField]
    public float Threshold = 0.5f;
}

/// <summary>
/// Checks if the target slot's entity health fraction is below a threshold.
/// Event-driven via DamageDealtEvent on the agent.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPCheckHealthLevelSensorComponent : CEGOAPSensorComponent<CEGOAPCheckHealthLevelSensorEntry>;

public sealed partial class CEGOAPCheckHealthLevelSensorSystem
    : CEGOAPSensorSystem<CEGOAPCheckHealthLevelSensorComponent, CEGOAPCheckHealthLevelSensorEntry>
{
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    // Runs after DamageableSystem applies the DamageDealtEvent to DamageableComponent.TotalDamage,
    // so the percentage read below reflects the post-hit value.
    [SubscribeLocalEvent(after: new[] { typeof(DamageableSystem) })]
    private void OnDamageDealt(Entity<CEGOAPCheckHealthLevelSensorComponent> ent, ref DamageDealtEvent args)
    {
        EvaluateAll(ent);
    }

    protected override bool Evaluate(EntityUid agent, CEGOAPCheckHealthLevelSensorEntry entry, CEGOAPSelectorResult target)
    {
        if (target.Entity is not { } entity)
            return false;

        var totalDamage = _damageable.GetTotalDamage(entity);
        if (!_mobThreshold.TryGetIncapPercentage(entity, totalDamage, out var percentage))
            return false;

        return (float) percentage.Value < entry.Threshold;
    }
}
