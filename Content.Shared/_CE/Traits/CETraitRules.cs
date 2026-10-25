using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Traits;

/// <summary>
/// Rules of a lobby trait selection, shared by the profile validation, the lobby editor and the spawn roll.
/// </summary>
public static class CETraitRules
{
    /// <summary>
    /// Chance of every picked trait to be granted on spawn, rolled independently per trait.
    /// </summary>
    public const float GrantChance = 0.5f;

    /// <summary>
    /// Sum of the costs of the given traits. Unknown traits count as zero.
    /// </summary>
    public static int GetBalance(IEnumerable<ProtoId<CETraitPrototype>> traits, IPrototypeManager protoMan)
    {
        var balance = 0;
        foreach (var trait in traits)
        {
            if (protoMan.TryIndex(trait, out var indexed))
                balance += indexed.Cost;
        }

        return balance;
    }

    /// <summary>
    /// Whether two traits can't be picked together. Symmetric.
    /// </summary>
    public static bool AreIncompatible(ProtoId<CETraitPrototype> a, ProtoId<CETraitPrototype> b, IPrototypeManager protoMan)
    {
        return protoMan.TryIndex(a, out var indexedA) && indexedA.Incompatible.Contains(b) ||
               protoMan.TryIndex(b, out var indexedB) && indexedB.Incompatible.Contains(a);
    }

    /// <summary>
    /// Whether a selection can be saved and granted: every trait exists, there are no conflicts and the balance is zero.
    /// Does not check unlock requirements - those depend on the player.
    /// </summary>
    public static bool IsSelectionValid(IReadOnlyCollection<ProtoId<CETraitPrototype>> traits, IPrototypeManager protoMan)
    {
        foreach (var trait in traits)
        {
            if (!protoMan.HasIndex(trait))
                return false;

            foreach (var other in traits)
            {
                if (AreIncompatible(trait, other, protoMan))
                    return false;
            }
        }

        return GetBalance(traits, protoMan) == 0;
    }
}
