using Content.Shared._CE.ZLevels.Flight;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.VehicleFlying;

/// <summary>
/// Controls CEFlyerComponent based on transport status
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CESharedZFlightSystem))]
public sealed partial class CEVehicleFlyerComponent : Component
{
}
