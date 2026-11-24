using System.Numerics;

namespace Content.Shared._CE.Alchemy.Effects;

/// <summary>
/// Modifier: scales the accumulated marker position per axis. The factor per axis is
/// <c>max(Min, 1 + PerUnit * amount)</c>. Covers amplifying, weakening, inversion and mirroring.
/// </summary>
public sealed partial class Scale : CEAlchemyEffect
{
    /// <summary>
    /// Factor growth per unit of the reagent on the X and Y axes.
    /// </summary>
    [DataField(required: true)]
    public Vector2 PerUnit;

    /// <summary>
    /// Lower bound of the factor. -1 allows full inversion, 0 allows collapsing to the center.
    /// </summary>
    [DataField]
    public float Min = -1f;

    public override void Apply(ref CEAlchemyState state, CEAlchemyApplyArgs args)
    {
        if (state.Carriers <= 0)
            return;

        var factorX = MathF.Max(Min, 1 + PerUnit.X * args.Amount);
        var factorY = MathF.Max(Min, 1 + PerUnit.Y * args.Amount);
        state.Pos = new Vector2(state.Pos.X * factorX, state.Pos.Y * factorY);
    }
}
