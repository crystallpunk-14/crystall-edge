using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roles;

/// <summary>
/// Added to mind role entities to tag that they are a thief. Also tracking their progression and rewards.
/// Stores configuration for how many skill points can be earned from stealing,
/// which skill point type to award, and scoring data used to measure performance.
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
