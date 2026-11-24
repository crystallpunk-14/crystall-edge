using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Alchemy.Data;

/// <summary>
/// A mixing step: everything poured in before the mix, in recipe units. The pour order is not stored:
/// the order of effects inside the step is decided by the reagents themselves.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CEAlchemyMixStep : CEAlchemyStep
{
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents = new();

    public void Add(ProtoId<ReagentPrototype> reagent, FixedPoint2 amount)
    {
        Reagents[reagent] = Reagents.GetValueOrDefault(reagent) + amount;
    }

    public override CEAlchemyStep Clone()
    {
        return new CEAlchemyMixStep { Reagents = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>(Reagents) };
    }

    public override bool Equals(CEAlchemyStep? other)
    {
        if (other is not CEAlchemyMixStep mix || mix.Reagents.Count != Reagents.Count)
            return false;

        foreach (var (reagent, amount) in Reagents)
        {
            if (!mix.Reagents.TryGetValue(reagent, out var otherAmount) || otherAmount != amount)
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        // Order independent: dictionaries with the same content must hash the same.
        var hash = 0;
        foreach (var (reagent, amount) in Reagents)
        {
            hash ^= HashCode.Combine(reagent, amount);
        }

        return hash;
    }
}
