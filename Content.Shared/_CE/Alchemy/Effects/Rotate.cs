using System.Numerics;

namespace Content.Shared._CE.Alchemy.Effects;

/// <summary>
/// Modifier: rotates the accumulated marker position around the center of the map.
/// </summary>
public sealed partial class Rotate : CEAlchemyEffect
{
    /// <summary>
    /// Degrees per unit of the reagent; positive is clockwise on screen (the Y axis points down),
    /// negative is counterclockwise.
    /// </summary>
    [DataField(required: true)]
    public float DegreesPerUnit;

    /// <summary>
    /// Largest rotation of one pour in degrees, in the direction of <see cref="DegreesPerUnit"/>. Zero means no limit.
    /// </summary>
    [DataField]
    public float MaxDegrees;

    public override void Apply(ref CEAlchemyState state, CEAlchemyApplyArgs args)
    {
        if (state.Carriers <= 0)
            return;

        var degrees = DegreesPerUnit * args.Amount;
        if (MaxDegrees > 0)
            degrees = Math.Clamp(degrees, -MaxDegrees, MaxDegrees);

        var angle = MathHelper.DegreesToRadians(degrees);
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        state.Pos = new Vector2(state.Pos.X * cos - state.Pos.Y * sin, state.Pos.X * sin + state.Pos.Y * cos);
    }
}
