using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared._CE.Roles.SecretRoleSeer;

/// <summary>
/// Gazes into the target's soul: the performer's mind learns to recognize the target's secret role,
/// and the target shivers.
/// </summary>
public sealed partial class CESoulGazeActionEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan JitterDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public float JitterAmplitude = 5f;

    [DataField]
    public float JitterFrequency = 4f;

    /// <summary>
    /// Played only to the performer.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
