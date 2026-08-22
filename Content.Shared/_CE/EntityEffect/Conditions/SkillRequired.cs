using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the target entity already has a specific skill learned.
/// </summary>
public sealed partial class SkillRequired : CEEntityConditionBase<SkillRequired>
{
    [DataField(required: true)]
    public ProtoId<CESkillPrototype> Skill;

    public override string GetDescription(IEntityManager entityManager, IPrototypeManager prototype)
    {
        var skillSystem = entityManager.System<CESharedSkillSystem>();
        var key = Inverted ? "ce-skill-req-not-prerequisite" : "ce-skill-req-prerequisite";
        return Loc.GetString(key, ("name", skillSystem.GetSkillName(Skill)));
    }
}

public sealed partial class CESkillRequiredConditionSystem : CEEntityConditionSystem<SkillRequired>
{
    [Dependency] private CESharedSkillSystem _skill = default!;

    protected override void Condition(ref CEEntityConditionEvent<SkillRequired> args)
    {
        args.Result = _skill.HaveSkill(args.Entity, args.Condition.Skill);
    }
}
