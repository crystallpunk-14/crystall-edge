namespace Content.Shared._DND.Equipment;

/// <summary>
/// Worn or held armor. A shield does not count as body armor.
/// </summary>
[RegisterComponent]
public sealed partial class DndArmorComponent : Component
{
    [DataField]
    public DndArmorCategory Category = DndArmorCategory.Light;

    /// <summary>
    /// Armor Class before Dexterity. Leather is 11. A shield adds 2 on top.
    /// </summary>
    [DataField]
    public int ArmorClass = 11;
}

public static class DndArmorRules
{
    public const int ShieldBonus = 2;
    public const int MediumDexterityCap = 2;

    public static int Score(DndArmorCategory category, int armorBase, int dexterityModifier, bool shield)
    {
        var score = armorBase;
        if (category == DndArmorCategory.Light)
            score += dexterityModifier;
        else if (category == DndArmorCategory.Medium)
            score += Math.Min(MediumDexterityCap, dexterityModifier);

        if (shield)
            score += ShieldBonus;

        return score;
    }
}
