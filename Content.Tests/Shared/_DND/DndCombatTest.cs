using System.Collections.Generic;
using Content.Shared._DND.Combat;
using NUnit.Framework;

namespace Content.Tests.Shared._DND;

[TestFixture]
public sealed class DndCombatTest
{
    [Test]
    public void NaturalTwentyHitsAnyArmorClass()
    {
        Assert.That(DndCombat.ResolveAttack(20, 0, 30), Is.EqualTo(DndAttackOutcome.CriticalHit));
    }

    [Test]
    public void NaturalOneMisses()
    {
        Assert.That(DndCombat.ResolveAttack(1, 10, 5), Is.EqualTo(DndAttackOutcome.Miss));
    }

    [Test]
    public void AttackHitsWhenTheTotalMeetsArmorClass()
    {
        Assert.That(DndCombat.ResolveAttack(10, 5, 15), Is.EqualTo(DndAttackOutcome.Hit));
        Assert.That(DndCombat.ResolveAttack(10, 5, 16), Is.EqualTo(DndAttackOutcome.Miss));
    }

    [Test]
    public void FinesseUsesTheHigherAbility()
    {
        Assert.That(DndCombat.AttackScore(DndAttackAbility.Finesse, 8, 16), Is.EqualTo(16));
        Assert.That(DndCombat.AttackScore(DndAttackAbility.Strength, 8, 16), Is.EqualTo(8));
        Assert.That(DndCombat.AttackBonus(16, 2), Is.EqualTo(5));
    }

    [Test]
    public void CriticalHitDoublesDiceButNotAFixedAmount()
    {
        Assert.That(DndCombat.DamageDiceCount(2, true), Is.EqualTo(4));
        Assert.That(DndCombat.DamageDiceCount(0, true), Is.EqualTo(0));
    }

    [Test]
    public void DamageCannotGoBelowZero()
    {
        Assert.That(DndCombat.ApplyDamage(1, -5, true, 0, false, false, false), Is.EqualTo(0));
    }

    [Test]
    public void FixedDamageAddsTheAbilityModifierOnlyWhenTheWeaponSaysSo()
    {
        Assert.That(DndCombat.ApplyDamage(1, 3, true, 0, false, false, false), Is.EqualTo(4));
        Assert.That(DndCombat.ApplyDamage(1, 3, false, 0, false, false, false), Is.EqualTo(1));
    }

    [Test]
    public void ResistanceThenVulnerabilityAndImmunity()
    {
        Assert.That(DndCombat.ApplyDamage(28, 0, false, -5, true, true, false), Is.EqualTo(22));
        Assert.That(DndCombat.ApplyDamage(5, 0, false, 0, true, false, false), Is.EqualTo(2));
        Assert.That(DndCombat.ApplyDamage(11, 0, false, 0, false, true, false), Is.EqualTo(22));
        Assert.That(DndCombat.ApplyDamage(30, 4, true, 0, true, true, true), Is.EqualTo(0));
    }

    [Test]
    public void OneResistanceFlagIsASingleHalving()
    {
        var types = new List<DndDamageType> { DndDamageType.Necrotic, DndDamageType.Necrotic };
        var resistant = true || DndCombat.Listed(types, DndDamageType.Necrotic);
        Assert.That(DndCombat.ApplyDamage(10, 0, false, 0, resistant, false, false), Is.EqualTo(5));
    }

    [Test]
    public void HitPointsUseTheHitDieConstitutionAndLevel()
    {
        Assert.That(DndCombat.HitPoints(6, 10, 1), Is.EqualTo(6));
        Assert.That(DndCombat.HitPoints(10, 14, 1), Is.EqualTo(12));
        Assert.That(DndCombat.HitPoints(6, 10, 2), Is.EqualTo(10));
        Assert.That(DndCombat.HitPoints(6, 1, 1), Is.EqualTo(1));
        Assert.That(DndCombat.HitPoints(12, 14, 5), Is.EqualTo(50));
    }

    [Test]
    public void FiveFeetIsOneTile()
    {
        Assert.That(DndCombat.Tiles(5), Is.EqualTo(1f));
        Assert.That(DndCombat.Tiles(10), Is.EqualTo(2f));
        Assert.That(DndCombat.Tiles(80), Is.EqualTo(16f));
        Assert.That(DndCombat.MeleeStoredRange(5), Is.EqualTo(0.5f));
    }
}
