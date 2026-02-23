using Content.Shared._CE.Skill.Effects;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Skill.Effects;

public sealed partial class WhiteAddMaxCharge : CESkillEffect
{
    [DataField]
    public float AdditionalCharge;

    public override void AddSkill(IEntityManager entManager, EntityUid target)
    {
        if (!entManager.TryGetComponent<BatteryComponent>(target, out var batteryComp))
            return;

        var batterySystem = entManager.System<SharedBatterySystem>();
        batterySystem.SetMaxCharge(target, batteryComp.MaxCharge + AdditionalCharge);
    }

    public override void RemoveSkill(IEntityManager entManager, EntityUid target)
    {
        if (!entManager.TryGetComponent<BatteryComponent>(target, out var batteryComp))
            return;

        var batterySystem = entManager.System<SharedBatterySystem>();
        batterySystem.SetMaxCharge(target, batteryComp.MaxCharge - AdditionalCharge);
    }

    public override string? GetName(IEntityManager entMagager, IPrototypeManager protoManager)
    {
        return null;
    }

    public override string? GetDescription(IEntityManager entMagager, IPrototypeManager protoManager, ProtoId<CESkillPrototype> skill)
    {
        return Loc.GetString("white-skill-desc-add-charge", ("charge", AdditionalCharge.ToString()));
    }
}
