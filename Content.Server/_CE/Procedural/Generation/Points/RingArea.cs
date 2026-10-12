using Robust.Shared.Random;

namespace Content.Server._CE.Procedural.Generation.Points;

/// <summary>
/// Ring (optionally a sector of it) around the center — keeps positions away from whatever sits in the middle.
/// Angles are in degrees, counter-clockwise from east.
/// </summary>
public sealed partial class RingArea : ICEPointArea
{
    [DataField(required: true)]
    public float MinRadius;

    [DataField(required: true)]
    public float MaxRadius;

    [DataField]
    public float MinAngle;

    [DataField]
    public float MaxAngle = 360f;

    public Vector2i Sample(IRobustRandom random)
    {
        var angle = MathHelper.DegreesToRadians(random.NextFloat(MinAngle, MaxAngle));

        // Lerping the squared radius spreads positions evenly over the ring's area instead of bunching them inward.
        var radius = MathF.Sqrt(MathHelper.Lerp(MinRadius * MinRadius, MaxRadius * MaxRadius, random.NextFloat()));

        return new Vector2i(
            (int) MathF.Round(MathF.Cos(angle) * radius),
            (int) MathF.Round(MathF.Sin(angle) * radius));
    }
}
