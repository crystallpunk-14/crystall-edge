using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Prototypes;

/// <summary>
/// A discovery rarity tier (Common, Rare, Legendary, etc). Drives both how far from a science
/// area's map center discoveries of this tier get placed, and their display color.
/// </summary>
[Prototype("scienceDiscoveryDifficulty")]
public sealed partial class CEScienceDiscoveryDifficultyPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public Color Color;

    /// <summary>
    /// The band of Chebyshev distance from the map's center, in tiles, that stars of this rarity
    /// get procedurally placed within.
    /// </summary>
    [DataField(required: true)]
    public MinMax SpawnDistance;
}
