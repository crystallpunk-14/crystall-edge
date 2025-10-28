using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicEnergy.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CEEnergyAlertComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> AlertType = "CEMagicEnergy";
}
