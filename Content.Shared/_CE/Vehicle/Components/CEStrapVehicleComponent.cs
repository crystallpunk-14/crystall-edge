using Robust.Shared.GameStates;

namespace Content.Shared._CE.Vehicle.Components;

/// <summary>
/// A <see cref="CEVehicleComponent"/> whose operator must be buckled to it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(CEVehicleSystem))]
public sealed partial class CEStrapVehicleComponent : Component;
