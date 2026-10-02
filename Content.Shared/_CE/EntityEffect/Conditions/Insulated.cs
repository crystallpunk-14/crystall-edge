using Content.Shared._CE.MagicEnergy.Components;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the summed <see cref="CEEnergyRadiationArmorComponent.Armor"/> of everything the entity wears
/// reaches <see cref="Min"/>. The default of 1.0 means a full insulating suit (e.g. brass insulator helmet + chestplate).
/// </summary>
public sealed partial class Insulated : CEEntityConditionBase<Insulated>
{
    [DataField]
    public float Min = 1f;
}

public sealed partial class CEInsulatedConditionSystem : CEEntityConditionSystem<Insulated>
{
    protected override void Condition(ref CEEntityConditionEvent<Insulated> args)
    {
        var ev = new CEEnergyRadiationDefenceCalculateEvent();
        RaiseLocalEvent(args.Entity, ev);

        var defence = 1f - ev.GetMultiplier();
        args.Result = defence + 0.001f >= args.Condition.Min;
    }
}
