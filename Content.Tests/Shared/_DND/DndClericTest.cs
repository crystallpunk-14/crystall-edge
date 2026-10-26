using Content.Shared._DND.Classes;
using Content.Shared._DND.Equipment;
using NUnit.Framework;

namespace Content.Tests.Shared._DND;

[TestFixture]
public sealed class DndClericTest
{
    [Test]
    public void ThaumaturgeAddsAtLeastOneToArcanaAndReligion()
    {
        Assert.That(DndClericRules.ThaumaturgeBonus(10), Is.EqualTo(1));
        Assert.That(DndClericRules.ThaumaturgeBonus(8), Is.EqualTo(1));
        Assert.That(DndClericRules.ThaumaturgeBonus(16), Is.EqualTo(3));
    }

    [Test]
    public void ThaumaturgeKnowsOneExtraCantrip()
    {
        Assert.That(DndClericRules.CantripCount(3, DndDivineOrders.Protector), Is.EqualTo(3));
        Assert.That(DndClericRules.CantripCount(3, DndDivineOrders.Thaumaturge), Is.EqualTo(4));
    }

    [Test]
    public void ChainShirtCapsDexterityAtTwoAndAShieldAddsTwo()
    {
        Assert.That(DndArmorRules.Score(DndArmorCategory.Medium, 13, 4, false), Is.EqualTo(15));
        Assert.That(DndArmorRules.Score(DndArmorCategory.Medium, 13, 1, true), Is.EqualTo(16));
        Assert.That(DndArmorRules.Score(DndArmorCategory.Medium, 13, -1, false), Is.EqualTo(12));
    }
}
