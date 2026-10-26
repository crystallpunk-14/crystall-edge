using System.Linq;
using Content.Shared._DND.Abilities;
using Content.Shared._DND.Classes;
using Content.Shared._DND.Equipment;
using Content.Shared._DND.Skills;
using Content.Shared._DND.Spells;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Lobby.UI;

public sealed partial class CEHumanoidProfileEditor
{
    private bool _updatingClassChoices;

    private void RefreshDndClasses()
    {
        DndClassList.RemoveAllChildren();

        var classes = _prototypeManager.EnumeratePrototypes<DndClassPrototype>()
            .OrderBy(dndClass => dndClass.Order)
            .ThenBy(dndClass => dndClass.ID)
            .ToList();

        var selectedId = Profile?.DndClass.Id ?? DndClassPrototype.Default;
        if (!_prototypeManager.HasIndex<DndClassPrototype>(selectedId))
            selectedId = DndClassPrototype.Default;

        var group = new ButtonGroup(isNoneSetAllowed: false);

        foreach (var dndClass in classes)
        {
            var button = new Button
            {
                Text = Loc.GetString(dndClass.Name),
                ToggleMode = true,
                Group = group,
                HorizontalExpand = true,
                Pressed = dndClass.ID == selectedId,
            };

            var classId = dndClass.ID;
            button.OnPressed += _ =>
            {
                if (!button.Pressed || Profile is null || Profile.DndClass.Id == classId)
                    return;

                if (!_prototypeManager.TryIndex<DndClassPrototype>(classId, out var selected))
                    return;

                Profile = DndClassChoices.Select(Profile, selected, _prototypeManager);
                SetDirty();
                RefreshDndClasses();
                UpdateDerivedStats();
            };

            DndClassList.AddChild(button);
            DndClassList.AddChild(new Label
            {
                Text = Loc.GetString(dndClass.Description),
                FontColorOverride = Color.Gray,
                Margin = new Thickness(4, 0, 0, 8),
            });

            if (dndClass.ID == selectedId)
                AddClassTraits(dndClass);
        }
    }

    private void AddClassTraits(DndClassPrototype dndClass)
    {
        if (dndClass.SpawnAsGhost)
            return;

        if (!string.IsNullOrEmpty(dndClass.PrimaryAbility.Id)
            && _prototypeManager.TryIndex(dndClass.PrimaryAbility, out DndAbilityPrototype? primary))
        {
            DndClassList.AddChild(TraitLabel(Loc.GetString("dnd-class-primary", ("ability", Loc.GetString(primary.Name)))));
        }

        if (dndClass.SavingThrows.Count > 0)
            DndClassList.AddChild(TraitLabel(Loc.GetString("dnd-class-saves", ("saves", JoinAbilities(dndClass.SavingThrows)))));

        var weapons = new List<DndWeaponTraining>(dndClass.WeaponTraining);
        var armor = new List<DndArmorCategory>(dndClass.ArmorTraining);
        if (Profile?.DivineOrder == DndDivineOrders.Protector && DndClassChoices.HasDivineOrder(dndClass, _prototypeManager))
        {
            if (!weapons.Contains(DndWeaponTraining.Martial))
                weapons.Add(DndWeaponTraining.Martial);
            if (!armor.Contains(DndArmorCategory.Heavy))
                armor.Add(DndArmorCategory.Heavy);
        }

        if (Profile?.PrimalOrder == DndPrimalOrders.Warden && DndClassChoices.HasPrimalOrder(dndClass, _prototypeManager))
        {
            if (!weapons.Contains(DndWeaponTraining.Martial))
                weapons.Add(DndWeaponTraining.Martial);
            if (!armor.Contains(DndArmorCategory.Medium))
                armor.Add(DndArmorCategory.Medium);
        }

        if (weapons.Count > 0)
            DndClassList.AddChild(TraitLabel(Loc.GetString("dnd-class-weapons", ("weapons", JoinTraining(weapons)))));

        if (armor.Count > 0)
            DndClassList.AddChild(TraitLabel(Loc.GetString("dnd-class-armor", ("armor", JoinArmor(armor)))));

        var features = false;
        foreach (var featureId in dndClass.Features)
        {
            if (!_prototypeManager.TryIndex(featureId, out DndFeaturePrototype? feature) || feature.Level > DndAbilityScores.StartingLevel)
                continue;

            if (!features)
            {
                DndClassList.AddChild(new Label
                {
                    Text = Loc.GetString("dnd-class-features"),
                    Margin = new Thickness(0, 8, 0, 2),
                });
                features = true;
            }

            DndClassList.AddChild(new Label { Text = Loc.GetString(feature.Name) });
            DndClassList.AddChild(new Label
            {
                Text = Loc.GetString(feature.Description),
                FontColorOverride = Color.Gray,
                Margin = new Thickness(4, 0, 0, 6),
            });
        }

        if (DndClassChoices.HasDivineOrder(dndClass, _prototypeManager))
            AddDivineOrder(dndClass);

        if (DndClassChoices.HasPrimalOrder(dndClass, _prototypeManager))
            AddPrimalOrder(dndClass);

        if (dndClass.AlwaysPrepared.Count > 0)
            AddAlwaysPrepared(dndClass);

        if (dndClass.SkillChoiceCount > 0)
            AddSkillChoices(dndClass);

        if (dndClass.InstrumentChoiceCount > 0)
            AddInstrumentChoices(dndClass);

        if (dndClass.CantripChoiceCount > 0)
            AddSpellChoices(dndClass, cantrips: true);

        if (dndClass.PreparedSpellCount > 0)
            AddSpellChoices(dndClass, cantrips: false);

        if (dndClass.EquipmentOptions.Count > 0)
            AddEquipmentChoices(dndClass);

        var masteryCount = DndClassFeatures.MasteryCount(dndClass, _prototypeManager, DndAbilityScores.StartingLevel);
        if (masteryCount > 0)
            AddMasteryChoices(masteryCount);
    }

    private void AddSkillChoices(DndClassPrototype dndClass)
    {
        DndClassList.AddChild(new Label
        {
            Text = Loc.GetString("dnd-class-skills", ("count", dndClass.SkillChoiceCount)),
            Margin = new Thickness(0, 8, 0, 2),
        });

        foreach (var skillId in DndClassChoices.SkillOptionIds(dndClass, _prototypeManager))
        {
            if (!_prototypeManager.TryIndex(skillId, out DndSkillPrototype? skill))
                continue;

            var selected = HasId(Profile?.SkillProficiencies, skill.ID);
            var button = new Button
            {
                Text = Loc.GetString(skill.Name),
                ToggleMode = true,
                Pressed = selected,
                HorizontalExpand = true,
            };

            var id = skill.ID;
            button.OnPressed += _ =>
            {
                if (_updatingClassChoices || Profile is null)
                    return;

                var skills = new List<ProtoId<DndSkillPrototype>>(Profile.SkillProficiencies);
                if (button.Pressed)
                {
                    if (HasId(skills, id))
                        return;

                    if (skills.Count >= dndClass.SkillChoiceCount)
                    {
                        _updatingClassChoices = true;
                        button.Pressed = false;
                        _updatingClassChoices = false;
                        return;
                    }

                    skills.Add(id);
                }
                else
                {
                    RemoveId(skills, id);
                }

                Profile = Profile.WithClassChoices(skills, Profile.EquipmentChoice, Profile.WeaponMasteries);
                SetDirty();
            };

            DndClassList.AddChild(button);
            DndClassList.AddChild(new Label
            {
                Text = Loc.GetString(skill.Description),
                FontColorOverride = Color.Gray,
                Margin = new Thickness(4, 0, 0, 4),
            });
        }
    }

    private void AddInstrumentChoices(DndClassPrototype dndClass)
    {
        DndClassList.AddChild(new Label
        {
            Text = Loc.GetString("dnd-class-instruments", ("count", dndClass.InstrumentChoiceCount)),
            Margin = new Thickness(0, 8, 0, 2),
        });

        foreach (var instrumentId in dndClass.InstrumentOptions)
        {
            if (!_prototypeManager.TryIndex(instrumentId, out DndInstrumentPrototype? instrument))
                continue;

            var id = instrument.ID;
            var button = new Button
            {
                Text = Loc.GetString(instrument.Name),
                ToggleMode = true,
                Pressed = HasId(Profile?.Instruments, id),
                HorizontalExpand = true,
            };

            button.OnPressed += _ =>
            {
                if (_updatingClassChoices || Profile is null)
                    return;

                var chosen = new List<ProtoId<DndInstrumentPrototype>>(Profile.Instruments);
                if (button.Pressed)
                {
                    if (HasId(chosen, id))
                        return;

                    if (chosen.Count >= dndClass.InstrumentChoiceCount)
                    {
                        _updatingClassChoices = true;
                        button.Pressed = false;
                        _updatingClassChoices = false;
                        return;
                    }

                    chosen.Add(id);
                }
                else
                {
                    RemoveId(chosen, id);
                }

                Profile = Profile.WithClassChoices(Profile.SkillProficiencies, Profile.EquipmentChoice, Profile.WeaponMasteries, chosen, Profile.Cantrips, Profile.PreparedSpells);
                SetDirty();
            };

            DndClassList.AddChild(button);
        }
    }

    private void AddDivineOrder(DndClassPrototype dndClass)
    {
        DndClassList.AddChild(new Label
        {
            Text = Loc.GetString("dnd-class-divine-order"),
            Margin = new Thickness(0, 8, 0, 2),
        });

        var selected = string.IsNullOrEmpty(Profile?.DivineOrder) ? DndDivineOrders.Protector : Profile.DivineOrder;
        var group = new ButtonGroup(isNoneSetAllowed: false);
        AddOrderButton(group, DndDivineOrders.Protector, "dnd-divine-order-protector", selected, primal: false);
        AddOrderButton(group, DndDivineOrders.Thaumaturge, "dnd-divine-order-thaumaturge", selected, primal: false);
    }

    private void AddPrimalOrder(DndClassPrototype dndClass)
    {
        DndClassList.AddChild(new Label
        {
            Text = Loc.GetString("dnd-class-primal-order"),
            Margin = new Thickness(0, 8, 0, 2),
        });

        var selected = string.IsNullOrEmpty(Profile?.PrimalOrder) ? DndPrimalOrders.Magician : Profile.PrimalOrder;
        var group = new ButtonGroup(isNoneSetAllowed: false);
        AddOrderButton(group, DndPrimalOrders.Magician, "dnd-primal-order-magician", selected, primal: true);
        AddOrderButton(group, DndPrimalOrders.Warden, "dnd-primal-order-warden", selected, primal: true);
    }

    private void AddAlwaysPrepared(DndClassPrototype dndClass)
    {
        var names = new List<string>();
        foreach (var id in dndClass.AlwaysPrepared)
        {
            if (_prototypeManager.TryIndex<DndSpellPrototype>(id, out var spell))
                names.Add(Loc.GetString(spell.Name));
        }

        if (names.Count == 0)
            return;

        DndClassList.AddChild(new Label
        {
            Text = Loc.GetString("dnd-class-always-prepared", ("spells", string.Join(", ", names))),
            Margin = new Thickness(0, 8, 0, 2),
        });
    }

    private void AddOrderButton(ButtonGroup group, string order, string text, string selected, bool primal)
    {
        var button = new Button
        {
            Text = Loc.GetString(text),
            ToggleMode = true,
            Group = group,
            Pressed = order == selected,
            HorizontalExpand = true,
        };

        button.OnPressed += _ =>
        {
            if (!button.Pressed || Profile is null)
                return;

            if (primal)
            {
                if (Profile.PrimalOrder == order)
                    return;

                Profile = Profile.WithClassChoices(Profile.SkillProficiencies, Profile.EquipmentChoice, Profile.WeaponMasteries, primalOrder: order);
            }
            else
            {
                if (Profile.DivineOrder == order)
                    return;

                Profile = Profile.WithClassChoices(Profile.SkillProficiencies, Profile.EquipmentChoice, Profile.WeaponMasteries, divineOrder: order);
            }
            if (_prototypeManager.TryIndex(Profile.DndClass, out DndClassPrototype? chosen))
                DndClassChoices.Normalize(Profile, chosen, _prototypeManager);

            SetDirty();
            RefreshDndClasses();
        };

        DndClassList.AddChild(button);
    }

    private void AddSpellChoices(DndClassPrototype dndClass, bool cantrips)
    {
        var count = cantrips
            ? DndClassChoices.CantripCount(dndClass, Profile, _prototypeManager)
            : dndClass.PreparedSpellCount;
        DndClassList.AddChild(new Label
        {
            Text = Loc.GetString(cantrips ? "dnd-class-cantrips" : "dnd-class-spells", ("count", count)),
            Margin = new Thickness(0, 8, 0, 2),
        });

        foreach (var spellId in DndClassChoices.SpellOptionIds(dndClass, _prototypeManager, cantrips))
        {
            if (!_prototypeManager.TryIndex<DndSpellPrototype>(spellId, out var spell))
                continue;

            var id = spell.ID;
            var selected = cantrips ? HasId(Profile?.Cantrips, id) : HasId(Profile?.PreparedSpells, id);
            var button = new Button
            {
                Text = Loc.GetString(spell.Name),
                ToggleMode = true,
                Pressed = selected,
                HorizontalExpand = true,
            };

            button.OnPressed += _ =>
            {
                if (_updatingClassChoices || Profile is null)
                    return;

                var chosen = new List<ProtoId<DndSpellPrototype>>(cantrips ? Profile.Cantrips : Profile.PreparedSpells);
                if (button.Pressed)
                {
                    if (HasId(chosen, id))
                        return;

                    if (chosen.Count >= count)
                    {
                        _updatingClassChoices = true;
                        button.Pressed = false;
                        _updatingClassChoices = false;
                        return;
                    }

                    chosen.Add(id);
                }
                else
                {
                    RemoveId(chosen, id);
                }

                Profile = cantrips
                    ? Profile.WithClassChoices(Profile.SkillProficiencies, Profile.EquipmentChoice, Profile.WeaponMasteries, Profile.Instruments, chosen, Profile.PreparedSpells)
                    : Profile.WithClassChoices(Profile.SkillProficiencies, Profile.EquipmentChoice, Profile.WeaponMasteries, Profile.Instruments, Profile.Cantrips, chosen);
                SetDirty();
            };

            DndClassList.AddChild(button);
            DndClassList.AddChild(new Label
            {
                Text = Loc.GetString(spell.Description),
                FontColorOverride = Color.Gray,
                Margin = new Thickness(4, 0, 0, 4),
            });
        }
    }

    private void AddEquipmentChoices(DndClassPrototype dndClass)
    {
        DndClassList.AddChild(new Label
        {
            Text = Loc.GetString("dnd-class-equipment"),
            Margin = new Thickness(0, 8, 0, 2),
        });

        var selected = Profile?.EquipmentChoice ?? "";
        var group = new ButtonGroup(isNoneSetAllowed: false);
        foreach (var option in dndClass.EquipmentOptions)
        {
            var button = new Button
            {
                Text = Loc.GetString(option.Name),
                ToggleMode = true,
                Group = group,
                Pressed = option.ID == selected,
                HorizontalExpand = true,
            };

            var id = option.ID;
            button.OnPressed += _ =>
            {
                if (!button.Pressed || Profile is null || Profile.EquipmentChoice == id)
                    return;

                Profile = Profile.WithClassChoices(Profile.SkillProficiencies, id, Profile.WeaponMasteries);
                SetDirty();
            };

            DndClassList.AddChild(button);
        }
    }

    private void AddMasteryChoices(int count)
    {
        DndClassList.AddChild(new Label
        {
            Text = Loc.GetString("dnd-class-mastery", ("count", count)),
            Margin = new Thickness(0, 8, 0, 2),
        });

        var kinds = _prototypeManager.EnumeratePrototypes<DndWeaponKindPrototype>()
            .Where(kind => kind.Melee)
            .OrderBy(kind => Loc.GetString(kind.Name))
            .ToList();

        foreach (var kind in kinds)
        {
            var selected = HasId(Profile?.WeaponMasteries, kind.ID);
            var button = new Button
            {
                Text = Loc.GetString("dnd-class-mastery-weapon", ("weapon", Loc.GetString(kind.Name)), ("mastery", MasteryName(kind.Mastery))),
                ToggleMode = true,
                Pressed = selected,
                HorizontalExpand = true,
            };

            var id = kind.ID;
            button.OnPressed += _ =>
            {
                if (_updatingClassChoices || Profile is null)
                    return;

                var masteries = new List<ProtoId<DndWeaponKindPrototype>>(Profile.WeaponMasteries);
                if (button.Pressed)
                {
                    if (HasId(masteries, id))
                        return;

                    if (masteries.Count >= count)
                    {
                        _updatingClassChoices = true;
                        button.Pressed = false;
                        _updatingClassChoices = false;
                        return;
                    }

                    masteries.Add(id);
                }
                else
                {
                    RemoveId(masteries, id);
                }

                Profile = Profile.WithClassChoices(Profile.SkillProficiencies, Profile.EquipmentChoice, masteries);
                SetDirty();
            };

            DndClassList.AddChild(button);
        }
    }

    private static Label TraitLabel(string text)
    {
        return new Label
        {
            Text = text,
            Margin = new Thickness(0, 0, 0, 2),
        };
    }

    private string JoinAbilities(IReadOnlyList<ProtoId<DndAbilityPrototype>> abilities)
    {
        var text = "";
        foreach (var abilityId in abilities)
        {
            if (!_prototypeManager.TryIndex(abilityId, out DndAbilityPrototype? ability))
                continue;

            if (text.Length > 0)
                text += ", ";

            text += Loc.GetString(ability.Name);
        }

        return text;
    }

    private string JoinTraining(IReadOnlyList<DndWeaponTraining> training)
    {
        var text = "";
        foreach (var entry in training)
        {
            if (text.Length > 0)
                text += ", ";

            text += Loc.GetString(entry == DndWeaponTraining.Martial ? "dnd-weapon-training-martial" : "dnd-weapon-training-simple");
        }

        return text;
    }

    private string JoinArmor(IReadOnlyList<DndArmorCategory> armor)
    {
        var text = "";
        foreach (var entry in armor)
        {
            if (text.Length > 0)
                text += ", ";

            text += Loc.GetString(ArmorName(entry));
        }

        return text;
    }

    private static string ArmorName(DndArmorCategory category)
    {
        return category switch
        {
            DndArmorCategory.Medium => "dnd-armor-medium",
            DndArmorCategory.Heavy => "dnd-armor-heavy",
            DndArmorCategory.Shield => "dnd-armor-shield",
            _ => "dnd-armor-light",
        };
    }

    private static string MasteryName(DndWeaponMastery mastery)
    {
        return Loc.GetString(mastery switch
        {
            DndWeaponMastery.Cleave => "dnd-mastery-cleave",
            DndWeaponMastery.Graze => "dnd-mastery-graze",
            DndWeaponMastery.Nick => "dnd-mastery-nick",
            DndWeaponMastery.Push => "dnd-mastery-push",
            DndWeaponMastery.Sap => "dnd-mastery-sap",
            DndWeaponMastery.Slow => "dnd-mastery-slow",
            DndWeaponMastery.Topple => "dnd-mastery-topple",
            DndWeaponMastery.Vex => "dnd-mastery-vex",
            _ => "dnd-mastery-none",
        });
    }

    private static bool HasId<T>(IReadOnlyList<ProtoId<T>>? ids, string id)
        where T : class, IPrototype
    {
        if (ids == null)
            return false;

        foreach (var entry in ids)
        {
            if (entry.Id == id)
                return true;
        }

        return false;
    }

    private static void RemoveId<T>(List<ProtoId<T>> ids, string id)
        where T : class, IPrototype
    {
        for (var i = ids.Count - 1; i >= 0; i--)
        {
            if (ids[i].Id == id)
                ids.RemoveAt(i);
        }
    }
}
