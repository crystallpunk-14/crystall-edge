using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class ReagentData : IEquatable<ReagentData>
{
    /// <summary>
    /// Convert to a string representation. This if for logging & debugging. This is not localized and should not be
    /// shown to players.
    /// </summary>
    public virtual string ToString(string prototype, FixedPoint2 quantity)
    {
        return $"{prototype}:{GetType().Name}:{quantity}";
    }

    /// <summary>
    /// Convert to a string representation. This if for logging & debugging. This is not localized and should not be
    /// shown to players.
    /// </summary>
    public virtual string ToString(string prototype)
    {
        return $"{prototype}:{GetType().Name}";
    }

    // CrystallEdge: data can change how fast the reagent is metabolized and what color it has
    /// <summary>
    /// Multiplier of the metabolism rate of a reagent carrying this data. Metabolism strength per unit scales with it.
    /// </summary>
    public virtual float MetabolismRateMultiplier => 1f;

    /// <summary>
    /// Color of a reagent carrying this data, or null to use the color of the reagent prototype.
    /// </summary>
    public virtual Color? GetColor(IPrototypeManager protoMan) => null;
    // CrystallEdge end

    public abstract bool Equals(ReagentData? other);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return Equals((ReagentData) obj);
    }

    public abstract override int GetHashCode();

    public abstract ReagentData Clone();
}
