using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill.Components;

/// <summary>
/// Marks an item as a book teaching a single skill. On MapInit, the entity's name and description
/// are overridden from the linked <see cref="CESkillPrototype"/>. Using the item in-hand (or the
/// "Study" alt-verb) starts a do-after that teaches the skill to the user.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CESkillBookComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<CESkillPrototype> Skill;
}
