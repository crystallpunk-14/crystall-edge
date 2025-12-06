using Content.Shared._CE.Vampire.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill.Restrictions;

public sealed partial class HigherVampire : CESkillRestriction
{
    public override bool HideFromUI => false;

    public override bool Check(IEntityManager entManager, EntityUid target)
    {
        if (!entManager.TryGetComponent<CEVampireComponent>(target, out var vampire))
            return false;

        return vampire.HigherVampire;
    }

    public override string GetDescription(IEntityManager entManager, IPrototypeManager protoManager)
    {
        return Loc.GetString("ce-skill-req-higher-vampire");
    }
}
