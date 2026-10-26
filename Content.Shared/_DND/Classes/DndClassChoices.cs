using Content.Shared._DND.Abilities;
using Content.Shared._DND.Equipment;
using Content.Shared._DND.Skills;
using Content.Shared._DND.Spells;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Classes;

/// <summary>
/// Keeps skill, equipment, and weapon-mastery picks inside what the chosen class offers.
/// </summary>
public static class DndClassChoices
{
    public static HumanoidCharacterProfile Select(
        HumanoidCharacterProfile profile,
        DndClassPrototype dndClass,
        IPrototypeManager prototypes)
    {
        var chosen = profile.WithDndClass(dndClass.ID);
        chosen.SkillProficiencies.Clear();
        chosen.WeaponMasteries.Clear();
        chosen.Instruments.Clear();
        chosen.Cantrips.Clear();
        chosen.PreparedSpells.Clear();
        chosen.DivineOrder = "";
        chosen.PrimalOrder = "";
        chosen.EquipmentChoice = "";
        Normalize(chosen, dndClass, prototypes);
        return chosen;
    }

    public static void Normalize(HumanoidCharacterProfile profile, DndClassPrototype? dndClass, IPrototypeManager prototypes)
    {
        var skills = Ids(profile.SkillProficiencies);
        var skillOptions = SkillOptionIds(dndClass, prototypes);
        NormalizeIds(skillOptions, dndClass?.SkillChoiceCount ?? 0, skills);

        var equipment = profile.EquipmentChoice;
        if (dndClass == null || dndClass.EquipmentOptions.Count == 0)
            equipment = "";
        else if (!HasEquipment(dndClass.EquipmentOptions, equipment))
            equipment = dndClass.EquipmentOptions[0].ID;

        var masteries = Ids(profile.WeaponMasteries);
        var allowed = MeleeKinds(prototypes);
        var defaults = new List<string>();
        if (dndClass != null)
        {
            foreach (var mastery in dndClass.DefaultMasteries)
            {
                if (Contains(allowed, mastery.Id))
                    defaults.Add(mastery.Id);
            }
        }

        var masteryCount = DndClassFeatures.MasteryCount(dndClass, prototypes, DndAbilityScores.StartingLevel);
        FilterIds(allowed, masteryCount, masteries);
        FillIds(defaults, masteryCount, masteries);
        FillIds(allowed, masteryCount, masteries);

        profile.SkillProficiencies.Clear();
        foreach (var skill in skills)
            profile.SkillProficiencies.Add(skill);

        profile.EquipmentChoice = equipment;
        profile.WeaponMasteries.Clear();
        foreach (var mastery in masteries)
            profile.WeaponMasteries.Add(mastery);

        var instruments = Ids(profile.Instruments);
        var instrumentOptions = new List<string>();
        if (dndClass != null)
        {
            foreach (var option in dndClass.InstrumentOptions)
                instrumentOptions.Add(option.Id);
        }

        NormalizeIds(instrumentOptions, dndClass?.InstrumentChoiceCount ?? 0, instruments);

        var order = profile.DivineOrder;
        if (!HasDivineOrder(dndClass, prototypes))
            order = "";
        else if (order != DndDivineOrders.Protector && order != DndDivineOrders.Thaumaturge)
            order = DndDivineOrders.Protector;

        profile.DivineOrder = order;

        var primal = profile.PrimalOrder;
        if (!HasPrimalOrder(dndClass, prototypes))
            primal = "";
        else if (primal != DndPrimalOrders.Magician && primal != DndPrimalOrders.Warden)
            primal = DndPrimalOrders.Magician;

        profile.PrimalOrder = primal;

        var cantrips = Ids(profile.Cantrips);
        var cantripOptions = SpellOptionIds(dndClass, prototypes, cantrips: true);
        NormalizeIds(cantripOptions, CantripCount(dndClass, profile, prototypes), cantrips);

        var prepared = Ids(profile.PreparedSpells);
        var preparedOptions = SpellOptionIds(dndClass, prototypes, cantrips: false);
        NormalizeIds(preparedOptions, dndClass?.PreparedSpellCount ?? 0, prepared);

        profile.Instruments.Clear();
        foreach (var instrument in instruments)
            profile.Instruments.Add(instrument);

        profile.Cantrips.Clear();
        foreach (var cantrip in cantrips)
            profile.Cantrips.Add(cantrip);

        profile.PreparedSpells.Clear();
        foreach (var spell in prepared)
            profile.PreparedSpells.Add(spell);
    }

    public static List<string> SkillOptionIds(DndClassPrototype? dndClass, IPrototypeManager prototypes)
    {
        var options = new List<string>();
        if (dndClass == null || dndClass.SkillChoiceCount <= 0)
            return options;

        if (dndClass.SkillOptions.Count == 0)
        {
            foreach (var skill in prototypes.EnumeratePrototypes<DndSkillPrototype>())
                options.Add(skill.ID);

            options.Sort(StringComparer.Ordinal);
            return options;
        }

        foreach (var option in dndClass.SkillOptions)
            options.Add(option.Id);

        return options;
    }

    public static List<string> SpellOptionIds(DndClassPrototype? dndClass, IPrototypeManager prototypes, bool cantrips)
    {
        var ranked = new List<(int preferred, int Order, string ID)>();
        if (dndClass == null || dndClass.SpellList == "")
            return new List<string>();

        foreach (var spell in prototypes.EnumeratePrototypes<DndSpellPrototype>())
        {
            if (!Listed(spell.Lists, dndClass.SpellList))
                continue;

            if (Listed(dndClass.AlwaysPrepared, spell.ID))
                continue;

            if (cantrips != (spell.Level == 0))
                continue;

            if (!cantrips && spell.Level > 1)
                continue;

            var preferred = Listed(spell.RecommendedFor, dndClass.SpellList) ? 0 : 1;
            ranked.Add((preferred, spell.Order, spell.ID));
        }

        ranked.Sort((a, b) =>
        {
            var preferred = a.preferred.CompareTo(b.preferred);
            if (preferred != 0)
                return preferred;

            var order = a.Order.CompareTo(b.Order);
            return order != 0 ? order : string.Compare(a.ID, b.ID, StringComparison.Ordinal);
        });

        var options = new List<string>(ranked.Count);
        foreach (var (_, _, id) in ranked)
            options.Add(id);

        return options;
    }

    public static bool HasDivineOrder(DndClassPrototype? dndClass, IPrototypeManager prototypes)
    {
        if (dndClass == null)
            return false;

        foreach (var featureId in dndClass.Features)
        {
            if (prototypes.TryIndex(featureId, out DndFeaturePrototype? feature)
                && feature.Kind == DndFeatureKind.DivineOrder
                && feature.Level <= DndAbilityScores.StartingLevel)
                return true;
        }

        return false;
    }

    public static bool HasPrimalOrder(DndClassPrototype? dndClass, IPrototypeManager prototypes)
    {
        return HasFeature(dndClass, prototypes, DndFeatureKind.PrimalOrder);
    }

    public static int CantripCount(DndClassPrototype? dndClass, HumanoidCharacterProfile? profile, IPrototypeManager prototypes)
    {
        var count = dndClass?.CantripChoiceCount ?? 0;
        if (HasDivineOrder(dndClass, prototypes))
            return DndClericRules.CantripCount(count, profile?.DivineOrder);

        if (HasPrimalOrder(dndClass, prototypes))
            return DndDruidRules.CantripCount(count, profile?.PrimalOrder);

        return count;
    }

    private static bool HasFeature(DndClassPrototype? dndClass, IPrototypeManager prototypes, DndFeatureKind kind)
    {
        if (dndClass == null)
            return false;

        foreach (var featureId in dndClass.Features)
        {
            if (prototypes.TryIndex(featureId, out DndFeaturePrototype? feature)
                && feature.Kind == kind
                && feature.Level <= DndAbilityScores.StartingLevel)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Drops ids that are not offered, drops duplicates, trims to <paramref name="count"/>, then fills from <paramref name="options"/>.
    /// </summary>
    public static void NormalizeIds(IReadOnlyList<string> options, int count, List<string> selected)
    {
        FilterIds(options, count, selected);
        FillIds(options, count, selected);
    }

    public static void FilterIds(IReadOnlyList<string> options, int count, List<string> selected)
    {
        if (count < 0)
            count = 0;

        for (var i = selected.Count - 1; i >= 0; i--)
        {
            if (!Contains(options, selected[i]))
                selected.RemoveAt(i);
        }

        for (var i = 0; i < selected.Count; i++)
        {
            for (var j = selected.Count - 1; j > i; j--)
            {
                if (selected[j] == selected[i])
                    selected.RemoveAt(j);
            }
        }

        while (selected.Count > count)
            selected.RemoveAt(selected.Count - 1);
    }

    public static void FillIds(IReadOnlyList<string> options, int count, List<string> selected)
    {
        foreach (var option in options)
        {
            if (selected.Count >= count)
                break;

            if (!Contains(selected, option))
                selected.Add(option);
        }
    }

    private static List<string> Ids<T>(IReadOnlyList<ProtoId<T>> ids)
        where T : class, IPrototype
    {
        var list = new List<string>(ids.Count);
        foreach (var id in ids)
            list.Add(id.Id);

        return list;
    }

    private static List<string> MeleeKinds(IPrototypeManager prototypes)
    {
        var kinds = new List<string>();
        foreach (var kind in prototypes.EnumeratePrototypes<DndWeaponKindPrototype>())
        {
            if (!kind.Melee)
                continue;

            if (kind.Training != DndWeaponTraining.Simple && kind.Training != DndWeaponTraining.Martial)
                continue;

            kinds.Add(kind.ID);
        }

        kinds.Sort(StringComparer.Ordinal);
        return kinds;
    }

    private static bool HasEquipment(List<DndEquipmentOption> options, string id)
    {
        foreach (var option in options)
        {
            if (option.ID == id)
                return true;
        }

        return false;
    }

    private static bool Listed(IReadOnlyList<string> ids, string id)
    {
        return Contains(ids, id);
    }

    private static bool Contains(IReadOnlyList<string> ids, string id)
    {
        foreach (var entry in ids)
        {
            if (entry == id)
                return true;
        }

        return false;
    }
}
