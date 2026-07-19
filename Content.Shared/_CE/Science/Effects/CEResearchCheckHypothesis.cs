namespace Content.Shared._CE.Science.Effects;

/// <summary>
/// Finds the nearest achievement cell in the action's area that the acting player hasn't
/// researched yet, and writes the distance (in cells) from the action's coordinate to that
/// achievement as a fading readout on the map.
/// </summary>
public sealed partial class CEResearchCheckHypothesis : CEResearchActionEffectBase<CEResearchCheckHypothesis>
{
    /// <summary>
    /// How long the distance readout stays visible before fully fading out.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The hypothesis can't find achievements farther than this many cells away. If none are
    /// found within radius, the result comes back empty - the client shows a sad face instead of
    /// a distance.
    /// </summary>
    [DataField]
    public int MaxRadius = 10;
}
