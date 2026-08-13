using Content.Shared._CE.Knowledge.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Knowledge.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class CEKnowledgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CEKnowledgePrototype>> Known = new();

    public override bool SendOnlyToOwner => true;
}