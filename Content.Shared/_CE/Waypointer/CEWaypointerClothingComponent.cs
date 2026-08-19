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
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CEWaypointerPrototype>> WaypointerProtoIds;

    /// <summary>
    /// The slots that, when equipped into, will grant the waypointer effect.
    /// </summary>
    [DataField]
    public SlotFlags SlotFlags = SlotFlags.WITHOUT_POCKET;
}
