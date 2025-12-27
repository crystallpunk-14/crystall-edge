using Content.Shared._CE.ZLevels.Flight;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.VehicleFlying;

/// <summary>
/// Provide flying actions to operator
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CEVehicleFlightSystem))]
public sealed partial class CEVehicleFlyerComponent : Component
{
}
