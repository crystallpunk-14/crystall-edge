using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Ghost;

/// <summary>
/// Gives a ghost that can still return to its body an action to give that up in exchange for meta information.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CEGhostMetaSystem))]
public sealed partial class CEGhostMetaComponent : Component
{
    [DataField]
    public EntProtoId Action = "CEActionGhostForfeitReturn";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
