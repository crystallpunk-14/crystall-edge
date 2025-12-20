using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roles;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEThiefRoleComponent : BaseMindRoleComponent
{
    [DataField]
    public float MaxSkillPointsFromStealing = 5f;

    [DataField]
    public ProtoId<CESkillPointPrototype> SkillPointType = "Memory";

    /// <summary>
    /// This value is calculated during initialization by reading the number of values in the game
    /// </summary>
    [DataField]
    public float MaxScore = 0f;

    /// <summary>
    /// Previous best score from past days
    /// </summary>
    [DataField]
    public float PreviousBestScore = 0f;
}
