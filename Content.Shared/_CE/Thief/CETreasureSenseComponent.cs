using Content.Shared.Actions;
using Content.Shared._CE.Waypointer;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Thief;

/// <summary>
/// Marker granted to a thief while their treasure sense is toggled on.
/// Contributes CETreasureWaypointer to the owner's CEWaypointerComponent while present.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CETreasureSenseComponent : Component
{
    [DataField]
    public HashSet<ProtoId<CEWaypointerPrototype>> WaypointerProtoIds = new() { "CETreasureWaypointer" };
}

public sealed partial class CEThiefToggleTreasureSenseEvent : InstantActionEvent;
