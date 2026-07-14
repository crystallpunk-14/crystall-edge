using Robust.Shared.Serialization;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Plays a sprite animation (offset, rotation, scale, color) directly on the <see cref="CEEffectTarget.User"/> entity.
/// Only channels that have keyframes defined are animated; channels with empty lists are left untouched.
/// Original values are automatically restored when the animation finishes.
/// </summary>
/// <remarks>
/// Unlike <see cref="UsedEntityAnimation"/>, this effect does NOT spawn a clone entity — it modifies the user's
/// own <c>SpriteComponent</c> in-place.
/// </remarks>
public sealed partial class UserAnimation : CEEntityEffectBase<UserAnimation>
{
    public UserAnimation()
    {
        EffectTarget = CEEffectTarget.User;
    }

    /// <summary>
    /// Keyframes for animating the sprite's offset (position relative to entity origin) over time.
    /// If empty, the offset is not animated.
    /// </summary>
    [DataField]
    public List<CEOffsetKeyframe> OffsetAnimation = new();

    /// <summary>
    /// Keyframes for animating the sprite's rotation over time (in degrees).
    /// If empty, the rotation is not animated.
    /// </summary>
    [DataField]
    public List<CERotationKeyframe> RotationAnimation = new();

    /// <summary>
    /// Keyframes for animating the sprite's color / alpha over time.
    /// If empty, the color is not animated.
    /// </summary>
    [DataField]
    public List<CEColorKeyframe> ColorAnimation = new();

    /// <summary>
    /// The color to restore the sprite to after the color animation finishes.
    /// Defaults to opaque white, which is the standard sprite base color.
    /// Set this explicitly if the entity uses a non-white base color.
    /// </summary>
    [DataField]
    public Color RestoreColor = Color.White;

    /// <summary>
    /// Keyframes for animating the sprite's scale over time.
    /// If empty, the scale is not animated.
    /// </summary>
    [DataField]
    public List<CEScaleKeyFrame> ScaleAnimation = new();
}
