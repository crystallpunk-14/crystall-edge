namespace Content.Shared._DND.Combat;

/// <summary>
/// Which ability is added to an attack roll and, when the damage uses dice, to the damage roll.
/// </summary>
public enum DndAttackAbility
{
    /// <summary>
    /// Melee attacks.
    /// </summary>
    Strength = 0,

    /// <summary>
    /// Ranged attacks.
    /// </summary>
    Dexterity = 1,

    /// <summary>
    /// Use whichever of Strength or Dexterity is higher, for both the attack and the damage.
    /// </summary>
    Finesse = 2,
}
