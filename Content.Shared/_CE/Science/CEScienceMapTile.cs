using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Science;

/// <summary>
/// A single non-empty tile of a science area's research map.
/// Empty tiles are not stored anywhere - only tiles with actual content, like dead zones or stars.
/// Extend with new sealed subclasses to add new tile types with their own data.
/// </summary>
[Serializable, NetSerializable]
public abstract class CEScienceMapTile
{
}

[Serializable, NetSerializable]
public sealed class CEScienceDeadZoneTile : CEScienceMapTile
{
}

/// <summary>
/// An unopened star. Doesn't remember which discovery prototype's rarity band placed it here -
/// which discovery ends up on this tile is decided later, purely from the rarity's candidate
/// pool, when a player opens it.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEScienceStarTile(ProtoId<CEScienceDiscoveryDifficultyPrototype> rarity) : CEScienceMapTile
{
    public readonly ProtoId<CEScienceDiscoveryDifficultyPrototype> Rarity = rarity;
}

/// <summary>
/// A star that's been opened: carries the candidate discoveries offered to whoever opened it.
/// Since this replaces the tile on the shared, round-wide map, every player who has this
/// coordinate researched sees the same offer.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEScienceOfferedStarTile(
    ProtoId<CEScienceDiscoveryDifficultyPrototype> rarity,
    List<ProtoId<CEScienceDiscoveryPrototype>> candidates) : CEScienceMapTile
{
    public readonly ProtoId<CEScienceDiscoveryDifficultyPrototype> Rarity = rarity;
    public readonly List<ProtoId<CEScienceDiscoveryPrototype>> Candidates = candidates;
}

[Serializable, NetSerializable]
public sealed class CEScienceDiscoveryTile(ProtoId<CEScienceDiscoveryPrototype> discovery) : CEScienceMapTile
{
    public readonly ProtoId<CEScienceDiscoveryPrototype> Discovery = discovery;
}
