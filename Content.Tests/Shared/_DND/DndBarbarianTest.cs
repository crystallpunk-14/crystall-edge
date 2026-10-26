using System;
using System.Collections.Generic;
using Content.Shared._DND.Classes;
using Content.Shared._DND.Combat;
using Content.Shared._DND.Features;
using NUnit.Framework;

namespace Content.Tests.Shared._DND;

[TestFixture]
public sealed class DndBarbarianTest
{
    [Test]
    public void UnarmoredDefenseAddsDexterityAndConstitution()
    {
        Assert.That(DndRageRules.UnarmoredArmorClass(14, 16), Is.EqualTo(15));
        Assert.That(DndRageRules.UnarmoredArmorClass(10, 10), Is.EqualTo(10));
        Assert.That(DndRageRules.UnarmoredArmorClass(8, 8), Is.EqualTo(8));
    }

    [Test]
    public void RageUsesStrengthOnAFinesseTie()
    {
        Assert.That(DndRageRules.UsesStrength(DndAttackAbility.Strength, 8, 16), Is.True);
        Assert.That(DndRageRules.UsesStrength(DndAttackAbility.Dexterity, 16, 8), Is.False);
        Assert.That(DndRageRules.UsesStrength(DndAttackAbility.Finesse, 16, 14), Is.True);
        Assert.That(DndRageRules.UsesStrength(DndAttackAbility.Finesse, 14, 16), Is.False);
        Assert.That(DndRageRules.UsesStrength(DndAttackAbility.Finesse, 14, 14), Is.True);
    }

    [Test]
    public void RageDamageIsAppliedBeforeResistance()
    {
        Assert.That(DndCombat.ApplyDamage(10, 0, false, 2, true, false, false), Is.EqualTo(6));
    }

    [Test]
    public void PhysicalResistanceDoesNotStack()
    {
        Assert.That(DndRageRules.IsPhysical(DndDamageType.Slashing), Is.True);
        Assert.That(DndRageRules.IsPhysical(DndDamageType.Fire), Is.False);
        Assert.That(DndCombat.ApplyDamage(11, 0, false, 0, true, false, false), Is.EqualTo(5));
    }

    [Test]
    public void GrazeDealsTheModifierAndCleaveDropsAPositiveOne()
    {
        Assert.That(DndRageRules.Graze(3), Is.EqualTo(3));
        Assert.That(DndRageRules.Graze(-1), Is.EqualTo(0));
        Assert.That(DndRageRules.CleaveAddsModifier(4), Is.False);
        Assert.That(DndRageRules.CleaveAddsModifier(-2), Is.True);
    }

    [Test]
    public void AdvantageKeepsTheHigherDie()
    {
        Assert.That(DndChecks.KeptRoll(20, 1, true, false), Is.EqualTo(20));
        Assert.That(DndChecks.KeptRoll(1, 20, false, true), Is.EqualTo(1));
        Assert.That(DndChecks.KeptRoll(8, 19, true, true), Is.EqualTo(8));
        Assert.That(DndCombat.ResolveAttack(DndChecks.KeptRoll(20, 2, true, false), 0, 30), Is.EqualTo(DndAttackOutcome.CriticalHit));
    }

    [Test]
    public void LevelOneRageIsTwoUsesAndTwoDamage()
    {
        var uses = new[] { 2, 2, 3 };
        var damage = new[] { 2, 2, 2 };
        Assert.That(DndClassTables.AtLevel(uses, 1, 0), Is.EqualTo(2));
        Assert.That(DndClassTables.AtLevel(damage, 1, 0), Is.EqualTo(2));
        Assert.That(DndClassTables.AtLevel(uses, 3, 0), Is.EqualTo(3));
    }

    [Test]
    public void RageRefreshStopsAtTenMinutes()
    {
        var start = TimeSpan.FromSeconds(0);
        var maxEnd = start + TimeSpan.FromSeconds(DndRageRules.MaxSeconds);
        var early = DndRageRules.RefreshExpiry(start, maxEnd);
        Assert.That(early, Is.EqualTo(TimeSpan.FromSeconds(DndRageRules.RoundSeconds)));

        var late = DndRageRules.RefreshExpiry(maxEnd - TimeSpan.FromSeconds(1), maxEnd);
        Assert.That(late, Is.EqualTo(maxEnd));
        Assert.That(DndRageRules.StillGoing(maxEnd, late, maxEnd), Is.False);
    }

    [Test]
    public void SkillChoiceKeepsTwoOfferedSkills()
    {
        var options = new List<string> { "AnimalHandling", "Athletics", "Intimidation", "Nature", "Perception", "Survival" };
        var selected = new List<string> { "Perception", "Stealth", "Athletics", "Perception" };
        DndClassChoices.NormalizeIds(options, 2, selected);
        Assert.That(selected, Is.EqualTo(new List<string> { "Perception", "Athletics" }));

        selected.Clear();
        DndClassChoices.NormalizeIds(options, 2, selected);
        Assert.That(selected, Is.EqualTo(new List<string> { "AnimalHandling", "Athletics" }));
    }

    [Test]
    public void SaveDcUsesEightPlusTheModifierAndProficiency()
    {
        Assert.That(DndChecks.DifficultyClass(16, 2), Is.EqualTo(13));
        Assert.That(DndChecks.Bonus(14, true, 2), Is.EqualTo(4));
        Assert.That(DndChecks.Bonus(14, false, 2), Is.EqualTo(2));
    }

    [Test]
    public void ActionPaceIsSixSecondsAndABonusActionIsThree()
    {
        Assert.That(DndActionPace.ActionSeconds, Is.EqualTo(6f));
        Assert.That(DndActionPace.BonusActionSeconds, Is.EqualTo(3f));
    }
}
