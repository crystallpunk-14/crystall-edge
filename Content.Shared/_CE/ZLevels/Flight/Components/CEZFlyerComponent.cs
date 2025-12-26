/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels.Flight.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(CESharedZFlightSystem))]
public sealed partial class CEZFlyerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TargetMapHeight = 0;

    [DataField]
    public float FlightSpeed = 1.5f;

    [DataField]
    public float DefaultGravityIntensity = 1f;
}
