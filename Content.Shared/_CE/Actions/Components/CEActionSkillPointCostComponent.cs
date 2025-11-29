using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Restricts the use of this action, by spending user skillpoints
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEActionSkillPointCostComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CESkillPointPrototype> SkillPoint;

    [DataField]
    public FixedPoint2 Count = 1f;
}
