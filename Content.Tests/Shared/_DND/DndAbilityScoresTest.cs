using System.Collections.Generic;
using Content.Shared._DND.Abilities;
using NUnit.Framework;

namespace Content.Tests.Shared._DND;

[TestFixture]
public sealed class DndAbilityScoresTest
{
    [TestCase(1, -5)]
    [TestCase(2, -4)]
    [TestCase(3, -4)]
    [TestCase(4, -3)]
    [TestCase(5, -3)]
    [TestCase(6, -2)]
    [TestCase(7, -2)]
    [TestCase(8, -1)]
    [TestCase(9, -1)]
    [TestCase(10, 0)]
    [TestCase(11, 0)]
    [TestCase(12, 1)]
    [TestCase(13, 1)]
    [TestCase(14, 2)]
    [TestCase(15, 2)]
    [TestCase(16, 3)]
    [TestCase(17, 3)]
    [TestCase(18, 4)]
    [TestCase(19, 4)]
    [TestCase(20, 5)]
    [TestCase(21, 5)]
    [TestCase(22, 6)]
    [TestCase(23, 6)]
    [TestCase(24, 7)]
    [TestCase(25, 7)]
    [TestCase(26, 8)]
    [TestCase(27, 8)]
    [TestCase(28, 9)]
    [TestCase(29, 9)]
    [TestCase(30, 10)]
    public void ModifierRoundsDown(int score, int expected)
    {
        Assert.That(DndAbilityScores.Modifier(score), Is.EqualTo(expected));
    }

    [TestCase(1, 2)]
    [TestCase(4, 2)]
    [TestCase(5, 3)]
    [TestCase(8, 3)]
    [TestCase(9, 4)]
    [TestCase(12, 4)]
    [TestCase(13, 5)]
    [TestCase(16, 5)]
    [TestCase(17, 6)]
    [TestCase(20, 6)]
    public void ProficiencyBonusFollowsLevel(int level, int expected)
    {
        Assert.That(DndAbilityScores.ProficiencyBonus(level), Is.EqualTo(expected));
    }

    [TestCase(1, 5)]
    [TestCase(8, 9)]
    [TestCase(10, 10)]
    [TestCase(11, 10)]
    [TestCase(14, 12)]
    [TestCase(20, 15)]
    public void BaseArmorClassUsesDexterity(int dexterity, int expected)
    {
        Assert.That(DndAbilityScores.BaseArmorClass(dexterity), Is.EqualTo(expected));
    }

    [TestCase(8, 0)]
    [TestCase(9, 1)]
    [TestCase(10, 2)]
    [TestCase(11, 3)]
    [TestCase(12, 4)]
    [TestCase(13, 5)]
    [TestCase(14, 7)]
    [TestCase(15, 9)]
    public void PointBuyCostMatchesTheTable(int score, int expected)
    {
        Assert.That(DndAbilityScores.TryPointBuyCost(score, out var cost), Is.True);
        Assert.That(cost, Is.EqualTo(expected));
    }

    [Test]
    public void StandardArrayCostsTheFullPointBuyBudget()
    {
        var ids = new[] { "A", "B", "C", "D", "E", "F" };
        var array = DndAbilityScores.CopyStandardArray();
        var scores = new Dictionary<string, int>();
        for (var i = 0; i < ids.Length; i++)
            scores[ids[i]] = array[i];

        Assert.That(DndAbilityScores.IsStandardArray(array), Is.True);
        Assert.That(DndAbilityScores.PointBuySpent(ids, scores), Is.EqualTo(DndAbilityScores.PointBuyBudget));
    }

    [Test]
    public void PointBuyLowersScoresUntilTheBudgetFits()
    {
        var ids = new[] { "Str", "Dex", "Con", "Int", "Wis", "Cha" };
        var scores = new Dictionary<string, int>();
        foreach (var id in ids)
            scores[id] = 15;

        var normalized = DndAbilityScores.NormalizePointBuy(ids, scores);

        Assert.That(DndAbilityScores.PointBuySpent(ids, normalized), Is.LessThanOrEqualTo(DndAbilityScores.PointBuyBudget));
        foreach (var score in normalized.Values)
        {
            Assert.That(score, Is.InRange(DndAbilityScores.PointBuyMinimum, DndAbilityScores.PointBuyMaximum));
        }
    }
}
