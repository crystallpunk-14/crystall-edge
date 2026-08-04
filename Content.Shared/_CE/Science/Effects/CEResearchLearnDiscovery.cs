namespace Content.Shared._CE.Science.Effects;

/// <summary>
/// Teaches the actor the knowledge linked to the already-resolved discovery cell at the action's
/// coordinate. Unlike <see cref="CEResearchOpenStar"/>, this targets a
/// <see cref="CEScienceDiscoveryCell"/> - some other player already chose this discovery as
/// first finder, but anyone who researches this same coordinate afterward can still pay its own
/// cost to learn it too.
/// </summary>
public sealed partial class CEResearchLearnDiscovery : CEResearchActionEffectBase<CEResearchLearnDiscovery>
{
}
