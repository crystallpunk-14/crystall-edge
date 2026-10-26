using Content.Shared._DND.Abilities;
using Content.Shared._DND.Backgrounds;
using Content.Shared._DND.Classes;
using Content.Shared._DND.Equipment;
using Content.Shared._DND.Features;
using Content.Shared._DND.Spells;
using Content.Shared.Actions;
using Content.Shared.Charges.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._DND.Features;

/// <summary>
/// Grants a class's level 1 features, training, and starting equipment when the body spawns.
/// </summary>
public sealed partial class DndFeatureSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedStorageSystem _storage = default!;

    [SubscribeLocalEvent]
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!_prototypes.TryIndex(ev.Profile.DndClass, out DndClassPrototype? dndClass) || dndClass.SpawnAsGhost)
            return;

        var level = DndAbilityScores.StartingLevel;
        var grantedRage = false;
        var grantedMastery = false;
        var grantedInspiration = false;
        var grantedSpells = false;
        var grantedOrder = false;
        var grantedPrimal = false;

        if (dndClass.SavingThrows.Count > 0 || dndClass.WeaponTraining.Count > 0 || dndClass.ArmorTraining.Count > 0 || ev.Profile.SkillProficiencies.Count > 0)
        {
            var training = EnsureComp<DndTrainingComponent>(ev.Mob);
            training.SavingThrows.Clear();
            training.Skills.Clear();
            training.Weapons.Clear();
            training.Armor.Clear();

            foreach (var save in dndClass.SavingThrows)
                training.SavingThrows.Add(save);

            foreach (var skill in ev.Profile.SkillProficiencies)
                training.Skills.Add(skill);

            foreach (var weapon in dndClass.WeaponTraining)
                training.Weapons.Add(weapon);

            foreach (var armor in dndClass.ArmorTraining)
                training.Armor.Add(armor);

            Dirty(ev.Mob, training);
        }

        foreach (var featureId in dndClass.Features)
        {
            if (!_prototypes.TryIndex(featureId, out DndFeaturePrototype? feature) || feature.Level > level)
                continue;

            switch (feature.Kind)
            {
                case DndFeatureKind.Rage:
                    var rage = EnsureComp<DndRageComponent>(ev.Mob);
                    rage.MaxUses = DndClassTables.AtLevel(feature.UsesByLevel, level, 2);
                    rage.Uses = rage.MaxUses;
                    rage.DamageBonus = DndClassTables.AtLevel(feature.DamageByLevel, level, 2);
                    rage.Active = false;
                    Dirty(ev.Mob, rage);
                    grantedRage = true;
                    break;
                case DndFeatureKind.UnarmoredDefense:
                    EnsureComp<DndUnarmoredDefenseComponent>(ev.Mob);
                    break;
                case DndFeatureKind.WeaponMastery:
                    var mastery = EnsureComp<DndWeaponMasteryComponent>(ev.Mob);
                    mastery.Kinds.Clear();
                    if (ev.Profile.WeaponMasteries.Count > 0)
                    {
                        foreach (var kind in ev.Profile.WeaponMasteries)
                            mastery.Kinds.Add(kind);
                    }
                    else
                    {
                        foreach (var kind in dndClass.DefaultMasteries)
                            mastery.Kinds.Add(kind);
                    }

                    grantedMastery = true;
                    break;
                case DndFeatureKind.BardicInspiration:
                {
                    var inspiration = EnsureComp<DndBardicInspirationSourceComponent>(ev.Mob);
                    inspiration.DieSides = DndClassTables.AtLevel(feature.DieByLevel, level, 6);
                    inspiration.MaxUses = DndBardRules.InspirationUses(FinalScore(ev.Profile, DndAbilityPrototype.CharismaId));
                    inspiration.Uses = inspiration.MaxUses;
                    Dirty(ev.Mob, inspiration);
                    grantedInspiration = true;
                    break;
                }
                case DndFeatureKind.Spellcasting:
                {
                    var casting = EnsureComp<DndSpellcastingComponent>(ev.Mob);
                    casting.Ability = dndClass.PrimaryAbility;
                    casting.Focus = dndClass.SpellFocus;
                    casting.SpellList = dndClass.SpellList;
                    casting.ReprepareOnLongRest = dndClass.ReprepareOnLongRest;
                    casting.MaxPrepared = dndClass.PreparedSpellCount;
                    casting.MaxSlots = DndClassTables.AtLevel(feature.SlotsByLevel, level, 2);
                    casting.Slots = casting.MaxSlots;
                    casting.Cantrips.Clear();
                    casting.Prepared.Clear();
                    casting.AlwaysPrepared.Clear();
                    foreach (var cantrip in ev.Profile.Cantrips)
                        casting.Cantrips.Add(cantrip);
                    foreach (var prepared in ev.Profile.PreparedSpells)
                        casting.Prepared.Add(prepared);
                    foreach (var always in dndClass.AlwaysPrepared)
                        casting.AlwaysPrepared.Add(always);
                    Dirty(ev.Mob, casting);
                    grantedSpells = true;
                    break;
                }
                case DndFeatureKind.DivineOrder:
                    grantedOrder = true;
                    break;
                case DndFeatureKind.Druidic:
                    EnsureComp<DndDruidicComponent>(ev.Mob);
                    break;
                case DndFeatureKind.PrimalOrder:
                    grantedPrimal = true;
                    break;
            }
        }

        if (grantedOrder)
            ApplyDivineOrder(ev.Mob, ev.Profile);

        if (grantedPrimal)
            ApplyPrimalOrder(ev.Mob, ev.Profile);

        if (grantedRage && TryComp<DndRageComponent>(ev.Mob, out var raging))
        {
            var action = AddAction(ev.Mob, DndActions.Rage);
            if (action != null)
            {
                raging.Action = action;
                _actions.SetUseDelay(action.Value, TimeSpan.FromSeconds(DndActionPace.BonusActionSeconds));
                _charges.SetMaxCharges(action.Value, raging.MaxUses);
                _charges.SetCharges(action.Value, raging.Uses);
            }
        }

        if (grantedMastery && TryComp<DndWeaponMasteryComponent>(ev.Mob, out var masteryComp))
        {
            var drill = AddAction(ev.Mob, DndActions.WeaponDrill);
            if (drill != null)
            {
                masteryComp.Drill = drill;
                _actions.SetUseDelay(drill.Value, TimeSpan.FromSeconds(DndActionPace.ActionSeconds));
                _charges.SetMaxCharges(drill.Value, 1);
                _charges.SetCharges(drill.Value, 0);
            }
        }

        if (grantedInspiration && TryComp<DndBardicInspirationSourceComponent>(ev.Mob, out var inspiring))
        {
            var action = AddAction(ev.Mob, DndActions.Inspire);
            if (action != null)
            {
                inspiring.Action = action;
                _actions.SetUseDelay(action.Value, TimeSpan.FromSeconds(DndActionPace.BonusActionSeconds));
                _charges.SetMaxCharges(action.Value, inspiring.MaxUses);
                _charges.SetCharges(action.Value, inspiring.Uses);
            }
        }

        if (grantedSpells && TryComp<DndSpellcastingComponent>(ev.Mob, out var spells))
            GrantSpells(ev.Mob, spells);

        GiveEquipment(ev.Mob, dndClass, ev.Profile);
    }

    private EntityUid? AddAction(EntityUid mob, string action)
    {
        var prototype = action;
        return _actions.AddAction(mob, prototype);
    }

    private void GrantSpells(EntityUid mob, DndSpellcastingComponent casting)
    {
        foreach (var cantrip in casting.Cantrips)
            GrantSpell(mob, casting, cantrip, slot: false);

        foreach (var prepared in casting.Prepared)
        {
            if (ContainsSpell(casting.AlwaysPrepared, prepared))
                continue;

            GrantSpell(mob, casting, prepared, slot: true);
        }

        foreach (var always in casting.AlwaysPrepared)
            GrantSpell(mob, casting, always, slot: true);
    }

    private static bool ContainsSpell(List<ProtoId<DndSpellPrototype>> spells, ProtoId<DndSpellPrototype> spell)
    {
        foreach (var id in spells)
        {
            if (id == spell)
                return true;
        }

        return false;
    }

    private void GrantSpell(EntityUid mob, DndSpellcastingComponent casting, ProtoId<DndSpellPrototype> spell, bool slot)
    {
        if (!_prototypes.TryIndex(spell, out DndSpellPrototype? prototype))
            return;

        var actionId = "DndAction" + prototype.ID;
        var action = AddAction(mob, actionId);
        if (action == null)
            return;

        var delay = prototype.Casting == DndCastingTime.BonusAction
            ? DndActionPace.BonusActionSeconds
            : DndActionPace.ActionSeconds;
        _actions.SetUseDelay(action.Value, TimeSpan.FromSeconds(delay));
        var marked = EnsureComp<DndSpellActionComponent>(action.Value);
        marked.Spell = prototype.ID;
        if (!slot)
            return;

        casting.SlotActions.Add(action.Value);
        _charges.SetMaxCharges(action.Value, casting.MaxSlots);
        _charges.SetCharges(action.Value, casting.Slots);
    }

    private void ApplyDivineOrder(EntityUid mob, HumanoidCharacterProfile profile)
    {
        if (!TryComp<DndTrainingComponent>(mob, out var training))
            training = EnsureComp<DndTrainingComponent>(mob);

        if (profile.DivineOrder == DndDivineOrders.Thaumaturge)
        {
            var thaumaturge = EnsureComp<DndThaumaturgeComponent>(mob);
            thaumaturge.Bonus = DndClericRules.ThaumaturgeBonus(FinalScore(profile, DndAbilityPrototype.WisdomId));
            Dirty(mob, thaumaturge);
        }
        else
        {
            if (!training.Weapons.Contains(DndWeaponTraining.Martial))
                training.Weapons.Add(DndWeaponTraining.Martial);

            if (!training.Armor.Contains(DndArmorCategory.Heavy))
                training.Armor.Add(DndArmorCategory.Heavy);
        }

        Dirty(mob, training);
    }

    private void ApplyPrimalOrder(EntityUid mob, HumanoidCharacterProfile profile)
    {
        if (!TryComp<DndTrainingComponent>(mob, out var training))
            training = EnsureComp<DndTrainingComponent>(mob);

        if (profile.PrimalOrder == DndPrimalOrders.Warden)
        {
            if (!training.Weapons.Contains(DndWeaponTraining.Martial))
                training.Weapons.Add(DndWeaponTraining.Martial);

            if (!training.Armor.Contains(DndArmorCategory.Medium))
                training.Armor.Add(DndArmorCategory.Medium);
        }
        else
        {
            var magician = EnsureComp<DndThaumaturgeComponent>(mob);
            magician.Bonus = DndDruidRules.MagicianBonus(FinalScore(profile, DndAbilityPrototype.WisdomId));
            magician.FirstSkillId = "Arcana";
            magician.SecondSkillId = "Nature";
            magician.FirstSkill = "dnd-skill-arcana-name";
            magician.SecondSkill = "dnd-skill-nature-name";
            Dirty(mob, magician);
        }

        Dirty(mob, training);
    }

    private int FinalScore(HumanoidCharacterProfile profile, string abilityId)
    {
        var baseScore = profile.GetAbilityScore(abilityId, 10);
        if (!_prototypes.TryIndex(profile.DndBackground, out DndBackgroundPrototype? background))
            return baseScore;

        return DndBackgroundScores.FinalScore(
            baseScore,
            DndBackgroundScores.Bonus(abilityId, background.Abilities, profile.BackgroundIncrease, profile.BackgroundPlusTwo, profile.BackgroundPlusOne));
    }

    private void GiveEquipment(EntityUid mob, DndClassPrototype dndClass, HumanoidCharacterProfile profile)
    {
        var choice = profile.EquipmentChoice;
        DndEquipmentOption? selected = null;
        foreach (var option in dndClass.EquipmentOptions)
        {
            if (option.ID == choice)
                selected = option;
        }

        if (selected == null && dndClass.EquipmentOptions.Count > 0)
            selected = dndClass.EquipmentOptions[0];

        if (selected == null)
            return;

        var coordinates = Transform(mob).Coordinates;
        var pending = new List<EntityUid>();
        EntityUid? pack = null;
        foreach (var stack in selected.Items)
        {
            var amount = Math.Max(1, stack.Amount);
            for (var i = 0; i < amount; i++)
            {
                var item = Spawn(stack.Prototype, coordinates);
                if (pack == null && HasComp<StorageComponent>(item))
                {
                    pack = item;
                    _inventory.TryEquip(mob, item, "back", silent: true, force: true);
                    continue;
                }

                pending.Add(item);
            }
        }

        if (choice != "Gold" && profile.Instruments.Count > 0)
        {
            if (_prototypes.TryIndex(profile.Instruments[0], out DndInstrumentPrototype? instrument))
            {
                var item = Spawn(instrument.Entity, coordinates);
                pending.Add(item);
            }
        }

        foreach (var item in pending)
        {
            if (pack != null && _storage.Insert(pack.Value, item, out _))
                continue;

            _hands.TryPickupAnyHand(mob, item);
        }
    }
}
