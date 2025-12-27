using Content.Shared._CE.ZLevels.Flight;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.FlyerHands;

/// <summary>
/// Prevents the use of hands during flight
/// </summary>
[RegisterComponent, NetworkedComponent,Access(typeof(CESharedZFlightSystem))]
public sealed partial class CEZFlyerBlockHandsComponent : Component
{
}
