using Content.Shared._CE.EntityEffect.Effects;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.AnimationController;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class CELoopAnimationData
{
    /// <summary>Keyframes for animating the sprite's offset over time.</summary>
    [DataField]
    public List<CEOffsetKeyframe> OffsetAnimation = new();

    /// <summary>Keyframes for animating the sprite's rotation over time (in degrees).</summary>
    [DataField]
    public List<CERotationKeyframe> RotationAnimation = new();

    /// <summary>Keyframes for animating the sprite's color/alpha over time.</summary>
    [DataField]
    public List<CEColorKeyframe> ColorAnimation = new();

    /// <summary>Keyframes for animating the sprite's scale over time.</summary>
    [DataField]
    public List<CEScaleKeyFrame> ScaleAnimation = new();
}
