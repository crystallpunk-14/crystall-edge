using Content.Shared._CE.Actions.Spells;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared._White.Actions.Spells;

public sealed partial class WhiteSpellConsumeCharge : CESpellEffect
{
    [DataField]
    public float Charge;

    [DataField]
    public bool Safe;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is not { } targetEntity)
            return;

        var batterySystem = entManager.System<SharedBatterySystem>();

        //First - used object
        if (args.Used is not null)
        {
            var chargeDelta = batterySystem.ChangeCharge(args.Used.Value, Charge, Safe);
            batterySystem.ChangeCharge(targetEntity, chargeDelta, Safe);
            return;
        }

        //Second - player
        if (args.User is not null)
        {
            var chargeDelta = batterySystem.ChangeCharge(args.User.Value, Charge, Safe);
            batterySystem.ChangeCharge(targetEntity, chargeDelta, Safe);
            return;
        }
    }
}
