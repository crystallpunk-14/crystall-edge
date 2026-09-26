namespace Content.Client._CE.TimedDespawnFadeout;

[RegisterComponent]
[Access(typeof(CETimedDespawnFadeoutSystem))]
public sealed partial class CETimedDespawnFadeoutComponent : Component
{
    /// <summary>
    /// Fraction of lifetime (0-1) where fade-in ends. 0 = no fade-in.
    /// </summary>
    [DataField]
    public float FadeInEnd;

    /// <summary>
    /// Fraction of lifetime (0-1) where fade-out begins. 0 = no hold, fades out from the start.
    /// </summary>
    [DataField]
    public float FadeOutStart;

    public float OriginalLifetime;
}
