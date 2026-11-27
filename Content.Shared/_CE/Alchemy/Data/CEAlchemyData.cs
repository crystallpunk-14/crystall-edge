using Content.Shared._CE.Alchemy.World;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Alchemy.Data;

/// <summary>
/// State of the <c>CEAlchemyMixture</c> reagent: the recipe and the tuning applied by devices.
/// Marker position, density and zone crossings are not stored, they are derived by replaying the recipe.
/// </summary>
[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class CEAlchemyData : ReagentData
{
    /// <summary>
    /// A recipe longer than this spoils the mixture into sludge.
    /// </summary>
    public const int MaxSteps = 24;

    [DataField]
    public List<CEAlchemyStep> Steps = new();

    /// <summary>
    /// Tuning from -1 (aged: weaker and longer) to +1 (distilled: stronger and shorter).
    /// </summary>
    [DataField]
    public FixedPoint2 Tune;

    /// <summary>
    /// Strength multiplier of the tuning, 2^(3 * Tune). Aged solutions are weaker and last longer,
    /// distilled ones are stronger and shorter: strength times duration stays constant.
    /// </summary>
    public float Multiplier => MathF.Pow(2f, 3f * Tune.Float());

    /// <summary>
    /// Sum of all poured amounts: the volume of the recipe in recipe units.
    /// </summary>
    public FixedPoint2 TotalAmount
    {
        get
        {
            var total = FixedPoint2.Zero;
            foreach (var step in Steps)
            {
                if (step is CEAlchemyMixStep mix)
                    total += mix.Amount;
            }

            return total;
        }
    }

    /// <summary>
    /// Appends a pour to the end of the recipe. If the solution was partly drunk, the poured amount is
    /// converted to recipe units: the recipe volume stays the reference, old steps are not touched.
    /// </summary>
    /// <param name="poured">Amount poured into the container.</param>
    /// <param name="currentQuantity">Amount of this solution in the container before the pour.</param>
    public void AddStep(ProtoId<ReagentPrototype> reagent, FixedPoint2 poured, FixedPoint2 currentQuantity)
    {
        var amount = poured;
        var total = TotalAmount;
        if (total > FixedPoint2.Zero && currentQuantity > FixedPoint2.Zero)
            amount = FixedPoint2.New(poured.Float() * total.Float() / currentQuantity.Float());

        Steps.Add(new CEAlchemyMixStep { Reagent = reagent, Amount = amount });
    }

    public override float MetabolismRateMultiplier => Multiplier;

    /// <summary>
    /// Color of the mixture: the colors of its active zones blended by their strength.
    /// Without effects it stays the default color of the reagent.
    /// </summary>
    public override Color? GetColor(IPrototypeManager protoMan)
    {
        var alchemy = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<CEAlchemySystem>();

        Color? result = null;
        var total = 0f;
        foreach (var hit in alchemy.Analyze(this))
        {
            if (hit.Strength <= 0)
                continue;

            total += hit.Strength;
            result = result is { } current
                ? Color.InterpolateBetween(current, hit.Zone.Color, hit.Strength / total)
                : hit.Zone.Color;
        }

        return result;
    }

    public override ReagentData Clone()
    {
        var steps = new List<CEAlchemyStep>(Steps.Count);
        foreach (var step in Steps)
        {
            steps.Add(step.Clone());
        }

        return new CEAlchemyData
        {
            Steps = steps,
            Tune = Tune,
        };
    }

    public override bool Equals(ReagentData? other)
    {
        if (other is not CEAlchemyData data || data.Tune != Tune || data.Steps.Count != Steps.Count)
            return false;

        for (var i = 0; i < Steps.Count; i++)
        {
            if (!Steps[i].Equals(data.Steps[i]))
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Tune);
        foreach (var step in Steps)
        {
            hash.Add(step);
        }

        return hash.ToHashCode();
    }
}
