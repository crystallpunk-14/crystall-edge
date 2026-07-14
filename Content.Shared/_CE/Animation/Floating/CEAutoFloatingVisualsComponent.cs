using System.Numerics;

namespace Content.Shared._CE.Animation.Floating;

/// <summary>
/// Makes an entity's sprite play a looped vertical floating animation client-side.
/// Driven per-frame by a sine wave with a random phase per entity so multiple
/// instances bob out of sync.
/// </summary>
[RegisterComponent]
public sealed partial class CEAutoFloatingVisualsComponent : Component
{
    /// <summary>
    /// How long it takes to go from the bottom of the animation to the top.
    /// Total cycle length is twice this value.
    /// </summary>
    [DataField]
    public float AnimationTime = 2f;

    [DataField]
    public Vector2 FloatingStartOffset = new(0, 0.4f);

    [DataField]
    public Vector2 FloatingOffset = new(0, 0.45f);

    /// <summary>
    /// Random phase offset in seconds, picked at component startup so that
    /// independently spawned entities bob out of sync.
    /// </summary>
    [ViewVariables]
    public float PhaseOffset;
}
