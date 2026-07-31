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
/// A single science area's map content, already filtered server-side down to only the coordinates
/// the requesting player has personally researched (that Researched set is data the filter is
/// built from - it never gets echoed back). Deliberately excludes the raw Researched set and the
/// player's points: both live on that player's own networked
/// <see cref="Content.Shared._CE.Science.Components.CEScienceResearchDataComponent"/> and are read
/// locally by the client, rather than round-tripped through this shared, per-table UI state - which
/// any player opening the same table could otherwise briefly observe a stale copy of.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEResearchTableAreaData(Dictionary<Vector2i, CEScienceMapCell> cells)
{
    public readonly Dictionary<Vector2i, CEScienceMapCell> Cells = cells;
}

/// <summary>
/// Full state sent once when the research table UI is opened (and re-sent after every research
/// action). Contains every science area's data, already scoped to the requesting player, so the
/// client can switch tabs without any further network round-trip. Deliberately carries nothing
/// about the acting player beyond that scoping - see <see cref="CEResearchTableAreaData"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEResearchTableState(
    Dictionary<ProtoId<CEScienceAreaPrototype>, CEResearchTableAreaData> areas) : BoundUserInterfaceState
{
    public readonly Dictionary<ProtoId<CEScienceAreaPrototype>, CEResearchTableAreaData> Areas = areas;
}

/// <summary>
/// Sent by the server after a "check hypothesis" action resolves. Deliberately not part of
/// <see cref="CEResearchTableState"/> - the result is short-lived and purely client-side
/// (fades out over <see cref="Duration"/>), so there's nothing worth persisting or resending on
/// every subsequent state refresh.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEResearchTableHypothesisResultMessage(
    ProtoId<CEScienceAreaPrototype> area,
    Vector2i coordinate,
    float? distance,
    TimeSpan duration) : BoundUserInterfaceMessage
{
    public readonly ProtoId<CEScienceAreaPrototype> Area = area;
    public readonly Vector2i Coordinate = coordinate;

    /// <summary>
    /// Null if no undiscovered achievement was found within the action's search radius.
    /// </summary>
    public readonly float? Distance = distance;

    public readonly TimeSpan Duration = duration;
}
