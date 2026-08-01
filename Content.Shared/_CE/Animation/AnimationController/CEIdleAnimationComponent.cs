using Robust.Shared.GameStates;

namespace Content.Shared._CE.AnimationController;

/// <summary>
/// Provides a fallback appearance key and a looping sway <see cref="UserAnimation"/> that are
/// applied by <see cref="CEAnimationControllerSystem"/> whenever the entity is <b>standing still</b>.
/// <para/>
/// Subscribes to <see cref="CECalculateCurrentAppearanceEvent"/> and
/// <see cref="CECalculateCurrentAnimationEvent"/> at priority <c>0</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEIdleAnimationComponent : Component
{
    /// <summary>
    /// Appearance key to activate while idle (e.g. "idle").
    /// Must match an entry in the entity's <c>GenericVisualizer</c> for
    /// <c>CEAnCEAnimationAppearanceVisuals.Key</c>.
    /// If <c>null</c>, no appearance change is proposed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? AppearanceKey;

    /// <summary>
    /// Looping sway animation to play on the entity's sprite while idle.
    /// If <c>null</c>, no animation is proposed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CELoopAnimationData? Animation;
}
