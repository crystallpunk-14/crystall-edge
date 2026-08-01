using Robust.Shared.GameStates;

namespace Content.Shared._CE.AnimationController;

/// <summary>
/// Provides a fallback appearance key and a looping sway <see cref="UserAnimation"/> that are
/// applied by <see cref="CEAnimationControllerSystem"/> whenever the entity is <b>moving</b>.
/// <para/>
/// Subscribes to <see cref="CECalculateCurrentAppearanceEvent"/> and
/// <see cref="CECalculateCurrentAnimationEvent"/> at priority <c>1</c>
/// (higher than <see cref="CEIdleAnimationComponent"/>'s priority <c>0</c>).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEWalkingAnimationComponent : Component
{
    /// <summary>
    /// Appearance key to activate while moving (e.g. "walk").
    /// Must match an entry in the entity's <c>GenericVisualizer</c> for
    /// <c>CEAnCEAnimationAppearanceVisuals.Key</c>.
    /// If <c>null</c>, no appearance change is proposed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? AppearanceKey;

    /// <summary>
    /// Looping sway animation to play on the entity's sprite while moving.
    /// Typically more intense than <see cref="CEIdleAnimationComponent.Animation"/>.
    /// If <c>null</c>, no animation is proposed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CELoopAnimationData? Animation;
}
