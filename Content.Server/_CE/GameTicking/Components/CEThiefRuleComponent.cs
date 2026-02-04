using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GameTicking.Components;

/// <summary>
/// Stores data for <see cref="CEThiefRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(CEThiefRuleSystem))]
public sealed partial class CEThiefRuleComponent : Component
{
    [DataField]
    public ProtoId<CESkillTreePrototype> ThiefSkillTree = "Thief";

    [DataField]
    public ProtoId<CESkillPointPrototype> SkillPointType = "Memory"; //TODO: fix duplicating with CEThiefRoleComponent
}
