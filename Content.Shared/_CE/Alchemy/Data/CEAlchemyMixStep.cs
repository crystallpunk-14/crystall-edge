using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Alchemy.Data;

/// <summary>
/// A recipe step: one reagent poured in. The order of steps in the recipe is the pour order.
/// The amount is in recipe units.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CEAlchemyMixStep : CEAlchemyStep
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override CEAlchemyStep Clone()
    {
        return new CEAlchemyMixStep { Reagent = Reagent, Amount = Amount };
    }

    public override bool Equals(CEAlchemyStep? other)
    {
        return other is CEAlchemyMixStep mix && mix.Reagent == Reagent && mix.Amount == Amount;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Reagent, Amount);
    }
}
