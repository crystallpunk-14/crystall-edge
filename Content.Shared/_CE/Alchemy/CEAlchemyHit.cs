using Content.Shared._CE.Alchemy.Prototypes;

namespace Content.Shared._CE.Alchemy;

/// <summary>
/// A zone that worked for an analyzed recipe and how strongly.
/// </summary>
public readonly record struct CEAlchemyHit(CEAlchemyZonePrototype Zone, float Strength);
