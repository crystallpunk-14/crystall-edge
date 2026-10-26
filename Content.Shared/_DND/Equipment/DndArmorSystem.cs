using Content.Shared._DND.Abilities;
using Content.Shared._DND.Classes;
using Content.Shared._DND.Combat;
using Content.Shared._DND.Features;
using Content.Shared._DND.Spells;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._DND.Equipment;

/// <summary>
/// Armor Class when Unarmored Defense is in effect, and the checks for body armor and heavy armor.
/// </summary>
public sealed partial class DndArmorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public int ArmorClass(EntityUid uid, DndAbilitiesComponent? abilities)
    {
        var score = BaseArmorClass(uid, abilities);
        if (TryComp<DndShieldOfFaithComponent>(uid, out var faith) && _timing.CurTime < faith.Until)
            score += faith.Bonus;

        return score;
    }

    private int BaseArmorClass(EntityUid uid, DndAbilitiesComponent? abilities)
    {
        if (TryComp<DndArmorClassComponent>(uid, out var armor))
            return armor.ArmorClass;

        abilities ??= CompOrNull<DndAbilitiesComponent>(uid);
        if (abilities == null)
            return DndCombat.DefaultArmorClass;

        var dexterity = DndAbilityScores.Modifier(abilities.Dexterity);
        var shield = WearingShield(uid);
        if (BodyArmor(uid) is { } worn)
            return DndArmorRules.Score(worn.Category, worn.ArmorClass, dexterity, shield);

        if (HasComp<DndUnarmoredDefenseComponent>(uid))
            return DndRageRules.UnarmoredArmorClass(abilities.Dexterity, abilities.Constitution) + (shield ? DndArmorRules.ShieldBonus : 0);

        return abilities.BaseArmorClass + (shield ? DndArmorRules.ShieldBonus : 0);
    }

    private bool WearingShield(EntityUid uid)
    {
        return AnyArmor(uid, armor => armor.Category == DndArmorCategory.Shield);
    }

    private DndArmorComponent? BodyArmor(EntityUid uid)
    {
        DndArmorComponent? found = null;
        var slots = _inventory.GetSlotEnumerator(uid);
        while (slots.MoveNext(out ContainerSlot? slot))
        {
            if (slot.ContainedEntity is { } worn && TryComp<DndArmorComponent>(worn, out var armor) && armor.Category != DndArmorCategory.Shield)
                found = armor;
        }

        if (found != null || !TryComp<HandsComponent>(uid, out var hands))
            return found;

        foreach (var held in _hands.EnumerateHeld((uid, hands)))
        {
            if (TryComp<DndArmorComponent>(held, out var armor) && armor.Category != DndArmorCategory.Shield)
                return armor;
        }

        return null;
    }

    public bool WearingBodyArmor(EntityUid uid)
    {
        return AnyArmor(uid, armor => armor.Category != DndArmorCategory.Shield);
    }

    public bool WearingHeavyArmor(EntityUid uid)
    {
        return AnyArmor(uid, armor => armor.Category == DndArmorCategory.Heavy);
    }

    /// <summary>
    /// Armor you are not trained with imposes disadvantage on attack rolls and on Strength and Dexterity saves.
    /// </summary>
    public bool HasPenalty(EntityUid uid)
    {
        if (!TryComp<DndTrainingComponent>(uid, out var training))
            return false;

        return AnyArmor(uid, armor => !Listed(training.Armor, armor.Category));
    }

    private bool AnyArmor(EntityUid uid, Func<DndArmorComponent, bool> match)
    {
        var slots = _inventory.GetSlotEnumerator(uid);
        while (slots.MoveNext(out ContainerSlot? slot))
        {
            if (slot.ContainedEntity is { } worn && Matches(worn, match))
                return true;
        }

        if (!TryComp<HandsComponent>(uid, out var hands))
            return false;

        foreach (var held in _hands.EnumerateHeld((uid, hands)))
        {
            if (Matches(held, match))
                return true;
        }

        return false;
    }

    private bool Matches(EntityUid item, Func<DndArmorComponent, bool> match)
    {
        return TryComp<DndArmorComponent>(item, out var armor) && match(armor);
    }

    private static bool Listed(List<DndArmorCategory> categories, DndArmorCategory category)
    {
        foreach (var entry in categories)
        {
            if (entry == category)
                return true;
        }

        return false;
    }
}
