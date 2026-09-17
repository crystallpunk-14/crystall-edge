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

    /// <summary>
    /// Source badge shown on a learned skill's card in the character menu, keyed by skill. A skill
    /// with no entry here shows no badge - most skills (research, skill books, admin grants) are
    /// unbadged by default.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CESkillPrototype>, CESkillDescriptor> Descriptors = new();
}

/// <summary>
/// Badge data for a learned skill's card in the character menu: source name, color, and
/// (optionally) a tooltip explaining it. Mirrors <c>CEObjectiveDescriptorComponent</c>, but skills
/// have no entity of their own to hang a component off of, so this is keyed data instead.
/// </summary>
[Serializable, DataDefinition]
public sealed partial class CESkillDescriptor
{
    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public LocId? Tooltip;
}
