using Robust.Shared.GameStates;

namespace Content.Shared._CE.Vehicle.Components;

/// <summary>
/// A <see cref="CEVehicleComponent"/> whose operator must be inside a specified container.
/// Note that the operator is the first to enter the container and won't be removed until they exit the container.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(CEVehicleSystem))]
public sealed partial class CEContainerVehicleComponent : Component
{
    /// <summary>
    /// The ID of the container for the operator.
    /// </summary>
    [DataField(required: true)]
    public string ContainerId;
}
