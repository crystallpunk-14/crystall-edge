using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Science;

[Serializable, NetSerializable]
public enum CEResearchTableUiKey
{
    Key,
}

/// <summary>
/// Sent when an action button is pressed for whatever coordinate the client currently has
/// selected locally (selection itself is never known to the server). The server re-validates the
/// action against the cell and the player before running its effects, then resends a full
/// <see cref="CEResearchTableState"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEResearchTableActionMessage(
    ProtoId<CEScienceAreaPrototype> area,
    Vector2i coordinate,
    ProtoId<CEResearchActionPrototype> action) : BoundUserInterfaceMessage
{
    public readonly ProtoId<CEScienceAreaPrototype> Area = area;
    public readonly Vector2i Coordinate = coordinate;
    public readonly ProtoId<CEResearchActionPrototype> Action = action;
}

/// <summary>
/// The player's view of a single science area's map: the content of the cells they've already
/// researched, and which coordinates those are. Unresearched cells are never sent, even if they
/// exist on the real map.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEResearchTableAreaData(
    Dictionary<Vector2i, CEScienceMapCell> cells,
    HashSet<Vector2i> researched)
{
    public readonly Dictionary<Vector2i, CEScienceMapCell> Cells = cells;
    public readonly HashSet<Vector2i> Researched = researched;
}

/// <summary>
/// Full state sent once when the research table UI is opened (and re-sent after every research
/// action). Contains every science area's data so the client can switch tabs without any
/// further network round-trip.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEResearchTableState(Dictionary<ProtoId<CEScienceAreaPrototype>, CEResearchTableAreaData> areas) : BoundUserInterfaceState
{
    public readonly Dictionary<ProtoId<CEScienceAreaPrototype>, CEResearchTableAreaData> Areas = areas;
}
