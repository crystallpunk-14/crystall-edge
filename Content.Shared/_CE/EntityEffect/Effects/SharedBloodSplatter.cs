using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Takes blood from the target and throws it to the floor along the effect angle
/// (<see cref="CEEntityEffectArgs.Angle"/>, see <see cref="Radial"/>) as a cleanable print in the blood's color.
/// Nothing happens if the target has no blood left.
/// </summary>
public sealed partial class BloodSplatter : CEEntityEffectBase<BloodSplatter>
{
    /// <summary>
    /// Blood taken from the target per splatter.
    /// </summary>
    [DataField]
    public FixedPoint2 Volume = 2;

    [DataField]
    public float MinDistance;

    [DataField]
    public float MaxDistance = 1f;

    /// <summary>
    /// Random deviation from the effect angle, up to this angle either way.
    /// </summary>
    [DataField]
    public Angle Spread = Angle.Zero;

    /// <summary>
    /// The RSI the print states are picked from. The art is expected to spray south.
    /// </summary>
    [DataField(required: true)]
    public ResPath Sprite;

    /// <summary>
    /// Each print picks one of these states at random.
    /// </summary>
    [DataField(required: true)]
    public List<string> States = new();

    [DataField]
    public CEDecalRotation Rotation = CEDecalRotation.Direction;

    /// <summary>
    /// Print alpha when the full <see cref="Volume"/> was taken.
    /// </summary>
    [DataField]
    public float MaxAlpha = 0.8f;

    [DataField]
    public EntProtoId Print = "CEBloodSplatter";

    /// <summary>
    /// Splash effect flying towards the print. Painted the blood's color through its appearance.
    /// </summary>
    [DataField]
    public EntProtoId? Vfx;

    /// <summary>
    /// How far out along the throw the splash effect spawns, so splashes don't pile up in the center.
    /// </summary>
    [DataField]
    public float VfxOffset = 0.5f;
}
