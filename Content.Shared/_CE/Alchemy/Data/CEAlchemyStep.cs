using Robust.Shared.Serialization;

namespace Content.Shared._CE.Alchemy.Data;

/// <summary>
/// One step of a recipe stored in <see cref="CEAlchemyData"/>.
/// Polymorphic: today only <see cref="CEAlchemyMixStep"/> exists, device steps may appear later.
/// Equality must be exact so identical recipes merge inside a solution.
/// </summary>
[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class CEAlchemyStep : IEquatable<CEAlchemyStep>
{
    public abstract bool Equals(CEAlchemyStep? other);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return Equals((CEAlchemyStep) obj);
    }

    public abstract override int GetHashCode();

    public abstract CEAlchemyStep Clone();
}
