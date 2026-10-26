namespace Content.Shared._DND.Combat;

/// <summary>
/// Kind of damage an attack deals. The type has no rules of its own; resistance, vulnerability, and immunity do.
/// </summary>
public enum DndDamageType
{
    Bludgeoning,
    Piercing,
    Slashing,
    Acid,
    Cold,
    Fire,
    Force,
    Lightning,
    Necrotic,
    Poison,
    Psychic,
    Radiant,
    Thunder,
}

public static class DndDamageTypes
{
    public static string NameId(DndDamageType type)
    {
        return type switch
        {
            DndDamageType.Piercing => "dnd-damage-piercing",
            DndDamageType.Slashing => "dnd-damage-slashing",
            DndDamageType.Acid => "dnd-damage-acid",
            DndDamageType.Cold => "dnd-damage-cold",
            DndDamageType.Fire => "dnd-damage-fire",
            DndDamageType.Force => "dnd-damage-force",
            DndDamageType.Lightning => "dnd-damage-lightning",
            DndDamageType.Necrotic => "dnd-damage-necrotic",
            DndDamageType.Poison => "dnd-damage-poison",
            DndDamageType.Psychic => "dnd-damage-psychic",
            DndDamageType.Radiant => "dnd-damage-radiant",
            DndDamageType.Thunder => "dnd-damage-thunder",
            _ => "dnd-damage-bludgeoning",
        };
    }

    /// <summary>
    /// Engine damage type that actually changes hit points. Several D&amp;D types share a bucket.
    /// Resistance is resolved on the D&amp;D type before this mapping.
    /// </summary>
    public static string Engine(DndDamageType type)
    {
        return type switch
        {
            DndDamageType.Piercing => "Piercing",
            DndDamageType.Slashing => "Slash",
            DndDamageType.Acid => "Caustic",
            DndDamageType.Cold => "Cold",
            DndDamageType.Fire => "Heat",
            DndDamageType.Lightning => "Shock",
            DndDamageType.Poison => "Poison",
            DndDamageType.Necrotic => "Poison",
            DndDamageType.Radiant => "Heat",
            DndDamageType.Psychic => "Cellular",
            _ => "Blunt",
        };
    }
}
