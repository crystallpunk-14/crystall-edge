using Robust.Shared.GameStates;

namespace Content.Shared._CE.Vehicle.Components;

/// <summary>
/// Tracking component for handling the operator of a given <see cref="CEVehicleComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CEVehicleSystem))]
public sealed partial class CEVehicleOperatorComponent : Component
{
    /// <summary>
    /// The vehicle we are currently operating.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;
}
