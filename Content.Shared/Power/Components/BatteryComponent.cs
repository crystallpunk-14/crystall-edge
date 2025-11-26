using Content.Shared.Power.EntitySystems;
using Content.Shared.Guidebook;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Battery node on the pow3r network. Needs other components to connect to actual networks.
/// </summary>
[AutoGenerateComponentState(fieldDeltas: true)] //CrystallEdge
[NetworkedComponent] //CrystallEdge
[RegisterComponent]
[Virtual]
[Access(typeof(SharedBatterySystem))]
public partial class BatteryComponent : Component
{
    public string SolutionName = "battery";

    /// <summary>
    /// Maximum charge of the battery in joules (ie. watt seconds)
    /// </summary>
    [DataField, AutoNetworkedField] //CrystallEdge autoNetworked
    [GuidebookData]
    public float MaxCharge;

    /// <summary>
    /// Current charge of the battery in joules (ie. watt seconds)
    /// </summary>
    [DataField("startingCharge"), AutoNetworkedField] //CrystallEdge autoNetworked
    public float CurrentCharge;

    /// <summary>
    /// The price per one joule. Default is 1 credit for 10kJ.
    /// </summary>
    [DataField]
    public float PricePerJoule = 0.0001f;
}
