using Content.Shared._CE.Knowledge.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Knowledge.Components;

/// <summary>
/// Marks an item as holding a piece of knowledge. On MapInit, the entity's name and description
/// are overridden from the linked <see cref="CEKnowledgePrototype"/>. Using the item in-hand (or
/// the "Study" alt-verb) starts a do-after that teaches the knowledge to the user.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEKnowledgeHolderComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<CEKnowledgePrototype> Knowledge;
}
