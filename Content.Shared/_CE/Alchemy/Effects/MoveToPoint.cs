using System.Numerics;

namespace Content.Shared._CE.Alchemy.Effects;

/// <summary>
/// Carrier: pulls the marker towards its point on the brewing map.
/// </summary>
public sealed partial class MoveToPoint : CEAlchemyEffect
{
    /// <summary>
    /// Base point on the map. The round seed shifts it, the shifted value lives in the round world.
    /// </summary>
    [DataField(required: true)]
    public Vector2 Pos;

    public override void Apply(ref CEAlchemyState state, CEAlchemyApplyArgs args)
    {
        var total = state.Carriers + args.Amount;
        if (total <= 0)
            return;

        var point = args.System.GetCarrierPosition(args.Reagent, this);
        state.Pos = (state.Pos * state.Carriers + point * args.Amount) / total;
        state.Carriers = total;
    }
}
