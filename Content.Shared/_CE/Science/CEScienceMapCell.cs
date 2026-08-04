using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Science;

/// <summary>
/// A single non-empty cell of a science area's research map.
/// Empty cells are not stored anywhere - only cells with actual content, like dead zones or stars.
/// Extend with new sealed subclasses to add new cell types with their own data.
/// </summary>
[Serializable, NetSerializable]
public abstract class CEScienceMapCell
{
    public abstract CEResearchCellKind Kind { get; }
}

[Serializable, NetSerializable]
public sealed class CEScienceDeadZoneCell : CEScienceMapCell
{
    public override CEResearchCellKind Kind => CEResearchCellKind.DeadZone;
}

/// <summary>
/// An unopened star. Doesn't remember which discovery prototype's rarity band placed it here -
/// which discovery ends up on this cell is decided later, purely from the rarity's candidate
/// pool, when a player opens it.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEScienceStarCell(ProtoId<CEScienceDiscoveryDifficultyPrototype> rarity) : CEScienceMapCell
{
    public readonly ProtoId<CEScienceDiscoveryDifficultyPrototype> Rarity = rarity;

    public override CEResearchCellKind Kind => CEResearchCellKind.Star;
}

/// <summary>
/// A star that's been opened: carries the candidate discoveries offered to whoever opened it.
/// Since this replaces the cell on the shared, round-wide map, every player who has this
/// coordinate researched sees the same offer.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEScienceOfferedStarCell(
    ProtoId<CEScienceDiscoveryDifficultyPrototype> rarity,
    List<ProtoId<CEScienceDiscoveryPrototype>> candidates) : CEScienceMapCell
{
    public readonly ProtoId<CEScienceDiscoveryDifficultyPrototype> Rarity = rarity;
    public readonly List<ProtoId<CEScienceDiscoveryPrototype>> Candidates = candidates;

    public override CEResearchCellKind Kind => CEResearchCellKind.OfferedStar;
}

[Serializable, NetSerializable]
public sealed class CEScienceDiscoveryCell(ProtoId<CEScienceDiscoveryPrototype> discovery) : CEScienceMapCell
{
    public readonly ProtoId<CEScienceDiscoveryPrototype> Discovery = discovery;

    public override CEResearchCellKind Kind => CEResearchCellKind.Discovery;
}
