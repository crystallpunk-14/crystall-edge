using Robust.Shared.GameStates;

namespace Content.Shared._CE.AnimationController;

/// <summary>
/// Marks an entity as managed by the animation controller.
/// The controller gathers fallback animation/appearance via
/// <see cref="CECalculateCurrentAnimationEvent"/> and <see cref="CECalculateCurrentAppearanceEvent"/>
/// raised from <see cref="CEAnimationControllerSystem.RefreshVisuals"/>.
/// <para/>
/// <see cref="CurrentAnimation"/> and <see cref="CurrentAppearanceKey"/> store the last resolved
/// values; client systems read them to drive looping playback.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEAnimationControllerComponent : Component
{
    /// <summary>
    /// The fallback loop animation currently active (resolved by the last <see cref="CECalculateCurrentAnimationEvent"/>).
    /// Null if no system proposed one.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CELoopAnimationData? CurrentAnimation;

    /// <summary>
    /// The fallback appearance key currently active (resolved by the last <see cref="CECalculateCurrentAppearanceEvent"/>).
    /// Null means no override — appearance stays at whatever the default is.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? CurrentAppearanceKey;
}
