using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill.Restrictions;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CESkillRestriction
{
    /// <summary>
    /// If true - this skill won't be shown in skill tree if user doesn't meet this restriction
    /// </summary>
    public virtual bool HideFromUI => false;

    public abstract bool Check(IEntityManager entManager, EntityUid target);

    public abstract string GetDescription(IEntityManager entManager, IPrototypeManager protoManager);
}
