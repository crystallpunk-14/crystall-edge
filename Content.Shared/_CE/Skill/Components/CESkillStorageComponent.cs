using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill.Components;

/// <summary>
/// Component that stores the skills learned by a player.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
[Access(typeof(CESharedSkillSystem))]
public sealed partial class CESkillStorageComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<CESkillPrototype>> LearnedSkills = new();
}
