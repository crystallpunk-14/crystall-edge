using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill.Effects;

public sealed partial class AddMaxStamina : CESkillEffect
{
    [DataField]
    public float AdditionalStamina = 0;

    public override void AddSkill(IEntityManager entManager, EntityUid target)
    {
        if (!entManager.TryGetComponent<StaminaComponent>(target, out var staminaComp))
            return;

        staminaComp.CritThreshold += AdditionalStamina;
        entManager.Dirty(target, staminaComp);
    }

    public override void RemoveSkill(IEntityManager entManager, EntityUid target)
    {
        if (!entManager.TryGetComponent<StaminaComponent>(target, out var staminaComp))
            return;

        staminaComp.CritThreshold -= AdditionalStamina;
        entManager.Dirty(target, staminaComp);
    }

    public override string? GetName(IEntityManager entManager, IPrototypeManager protoManager)
    {
        return null;
    }

    public override string? GetDescription(IEntityManager entManager, IPrototypeManager protoManager, ProtoId<CESkillPrototype> skill)
    {
        return Loc.GetString("ce-skill-desc-add-stamina", ("stamina", AdditionalStamina.ToString()));
    }
}
