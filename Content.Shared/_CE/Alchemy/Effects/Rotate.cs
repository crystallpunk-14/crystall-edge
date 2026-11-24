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

    public override void Apply(ref CEAlchemyState state, CEAlchemyApplyArgs args)
    {
        if (state.Carriers <= 0)
            return;

        var angle = MathHelper.DegreesToRadians(DegreesPerUnit * args.Amount);
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        state.Pos = new Vector2(state.Pos.X * cos - state.Pos.Y * sin, state.Pos.X * sin + state.Pos.Y * cos);
    }
}
