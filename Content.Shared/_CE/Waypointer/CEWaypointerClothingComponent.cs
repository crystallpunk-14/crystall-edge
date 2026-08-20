using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Waypointer;

/// <summary>
///  This is used for clothing that enables waypointers for the equipee.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEWaypointerClothingComponent : Component
{
    /// <summary>
    /// The prototype of the waypointer that this clothing will grant to the wearer.
    /// Contributed to CEWaypointerComponent's resolved set while equipped in a matching slot.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<CEWaypointerPrototype>> WaypointerProtoIds = new();

    /// <summary>
    /// The slots that, when equipped into, will grant the waypointer effect.
    /// </summary>
    [DataField]
    public SlotFlags SlotFlags = SlotFlags.WITHOUT_POCKET;
}
