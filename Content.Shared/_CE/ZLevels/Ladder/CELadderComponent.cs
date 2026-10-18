using Robust.Shared.Audio;

namespace Content.Shared._CE.ZLevels.Ladder;

/// <summary>
/// Wall-mounted ladder. Mobs touching its sensor fixture hold on to it: they slide down, hang in place
/// or climb towards the level above, depending on their movement input.
/// </summary>
[RegisterComponent, Access(typeof(CELadderSystem))]
public sealed partial class CELadderComponent : Component
{
    /// <summary>
    /// Vertical speed (z-levels per second) while sliding down without input.
    /// </summary>
    [DataField]
    public float DescendSpeed = 1.2f;

    /// <summary>
    /// Climbing speed as a fraction of the climber's current movement speed.
    /// </summary>
    [DataField]
    public float ClimbSpeedModifier = 0.5f;

    /// <summary>
    /// After climbing over the top, the ladder below lifts the climber only this high above the new level.
    /// </summary>
    [DataField]
    public float ExitHeight = 0.2f;

    /// <summary>
    /// A step sound is played every time the climber covers this much height.
    /// </summary>
    [DataField]
    public float StepInterval = 0.25f;

    [DataField]
    public SoundSpecifier? StepSound = new SoundCollectionSpecifier("FootstepWood");
}
