using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roles;

/// <summary>
/// Added to mind role entities to tag that they are a thief.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEThiefRoleComponent : BaseMindRoleComponent
{
    [DataField]
    public float MaxSkillPointsFromStealing = 3f;

    [DataField]
    public ProtoId<CESkillPointPrototype> SkillPointType = "Memory";
}
