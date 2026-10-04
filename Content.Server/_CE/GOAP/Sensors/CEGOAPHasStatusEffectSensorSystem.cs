using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GOAP.Sensors;

public sealed partial class CEGOAPHasStatusEffectSensorEntry
    : CEGOAPSensorEntry<CEGOAPHasStatusEffectSensorEntry, CEGOAPHasStatusEffectSensorComponent>
{
    /// <summary>
    /// Prototype ID of the status effect entity to check for.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId StatusEffect;
}

/// <summary>
/// Checks if the target slot's entity has a specific status effect active.
/// Event-driven: reacts to status effect applied/removed events on this entity.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPHasStatusEffectSensorComponent : CEGOAPSensorComponent<CEGOAPHasStatusEffectSensorEntry>;

public sealed partial class CEGOAPHasStatusEffectSensorSystem
    : CEGOAPSensorSystem<CEGOAPHasStatusEffectSensorComponent, CEGOAPHasStatusEffectSensorEntry>
{
    [Dependency] private StatusEffectsSystem _statusEffect = default!;

    [SubscribeLocalEvent]
    private void OnEffectApplied(ref StatusEffectAppliedEvent args)
    {
        if (TryComp<CEGOAPHasStatusEffectSensorComponent>(args.Target, out var sensor))
            EvaluateAll((args.Target, sensor));
    }

    [SubscribeLocalEvent]
    private void OnEffectRemoved(ref StatusEffectRemovedEvent args)
    {
        if (TryComp<CEGOAPHasStatusEffectSensorComponent>(args.Target, out var sensor))
            EvaluateAll((args.Target, sensor));
    }

    protected override bool Evaluate(EntityUid agent, CEGOAPHasStatusEffectSensorEntry entry, CEGOAPSelectorResult target)
    {
        return target.Entity is { } entity && _statusEffect.HasStatusEffect(entity, entry.StatusEffect);
    }
}
