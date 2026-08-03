namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Marks an item as a thaumaturgic magnifying glass. While held, the client periodically reveals
/// nearby <see cref="CEScientificInterestComponent"/> entities the local player hasn't studied yet
/// with a looping VFX (see <c>Content.Client._CE.Science.CEClientScienceSystem</c>), and clicking
/// it on one of them starts the same study <see cref="CEScientificInterestDoAfterEvent"/> the old
/// "Study" verb used to.
/// </summary>
[RegisterComponent]
public sealed partial class CEThaumaturgicMagnifyingGlassComponent : Component
{
    /// <summary>
    /// How often, while held, the client re-scans for nearby unstudied scientific interest. Matches
    /// CEScientificInterestVFX's full animation length (20 frames * 0.15s) so a fresh one spawns right
    /// as the previous one's loop and TimedDespawn finish, instead of overlapping or being cut off.
    /// </summary>
    [DataField]
    public TimeSpan RevealInterval = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Multiplies the target's own <see cref="CEScientificInterestComponent.Time"/> when the study
    /// do-after is started by clicking this glass on it. 1 = unchanged, 0.5 = twice as fast.
    /// </summary>
    [DataField]
    public float StudyTimeMultiplier = 1f;
}
