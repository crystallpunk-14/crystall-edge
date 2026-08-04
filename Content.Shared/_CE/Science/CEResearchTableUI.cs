using Content.Shared._CE.MagicEssence.Prototypes;
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
/// Sent when a player picks one of the candidates offered by an opened star. The server
/// re-validates the tile is still a <see cref="CEScienceOfferedStarTile"/> and that Discovery is
/// among its candidates.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEResearchTableChooseDiscoveryMessage(
    ProtoId<CEScienceAreaPrototype> area,
    Vector2i coordinate,
    ProtoId<CEScienceDiscoveryPrototype> discovery) : BoundUserInterfaceMessage
{
    public readonly ProtoId<CEScienceAreaPrototype> Area = area;
    public readonly Vector2i Coordinate = coordinate;
    public readonly ProtoId<CEScienceDiscoveryPrototype> Discovery = discovery;
}

/// <summary>
/// Sent when the player presses the merge button in the knowledge panel with two aspects selected.
/// The server re-validates that a recipe exists for this pair and that the actor can still afford
/// it (1 of each) before spending anything - the client-side check is only for the button's enabled
/// state.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEResearchTableMergeEssenceMessage(
    ProtoId<CEMagicEssenceTypePrototype> first,
    ProtoId<CEMagicEssenceTypePrototype> second) : BoundUserInterfaceMessage
{
    public readonly ProtoId<CEMagicEssenceTypePrototype> First = first;
    public readonly ProtoId<CEMagicEssenceTypePrototype> Second = second;
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
public sealed class CEResearchTableAreaData(Dictionary<Vector2i, CEScienceMapTile> tiles)
{
    public readonly Dictionary<Vector2i, CEScienceMapTile> Tiles = tiles;
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
