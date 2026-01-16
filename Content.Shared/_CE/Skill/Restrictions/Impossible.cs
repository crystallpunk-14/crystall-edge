using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill.Restrictions;

public sealed partial class Impossible : CESkillRestriction
{
    public override bool Check(IEntityManager entManager, EntityUid target)
    {
        return false;
    }

    public override string GetDescription(IEntityManager entManager, IPrototypeManager protoManager)
    {
        return Loc.GetString("ce-skill-req-impossible");
    }
}
