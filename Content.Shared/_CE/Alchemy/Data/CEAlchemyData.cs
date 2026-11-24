using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
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
