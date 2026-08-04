namespace Content.Shared._CE.Science.Effects;

/// <summary>
/// Rolls a 3-candidate discovery offer for the star cell at the action's coordinate, replacing it
/// with a <see cref="CEScienceOfferedStarCell"/>. Choosing one of the offered candidates is a
/// separate step (<see cref="Content.Shared._CE.Science.CEResearchTableChooseDiscoveryMessage"/>),
/// not part of this action's effects, since its cost varies per candidate.
/// </summary>
public sealed partial class CEResearchOpenStar : CEResearchActionEffectBase<CEResearchOpenStar>
{
}
