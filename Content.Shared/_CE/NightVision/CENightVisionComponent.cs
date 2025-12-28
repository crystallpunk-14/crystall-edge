using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CENightVisionComponent : Component
{
    [DataField]
    public EntityUid? LocalLightEntity = null;

    [DataField, AutoNetworkedField]
    public EntProtoId LightPrototype = "CENightVisionLight";

    [DataField, AutoNetworkedField]
    public EntProtoId ActionPrototype = "CEActionToggleNightVision";

    [DataField]
    public EntityUid? ActionEntity = null;
}
