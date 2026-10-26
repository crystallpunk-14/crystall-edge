using Content.Shared._DND.Equipment;
using Content.Shared._DND.Spells;
using NUnit.Framework;

namespace Content.Tests.Shared._DND;

[TestFixture]
public sealed class DndBardTest
{
    [Test]
    public void InspirationUsesAreTheCharismaModifierAndAtLeastOne()
    {
        Assert.That(DndBardRules.InspirationUses(10), Is.EqualTo(1));
        Assert.That(DndBardRules.InspirationUses(8), Is.EqualTo(1));
        Assert.That(DndBardRules.InspirationUses(16), Is.EqualTo(3));
    }

    [Test]
    public void ACantripDoesNotSpendASpellSlot()
    {
        Assert.That(DndBardRules.SlotsAfterCast(0, 2), Is.EqualTo(2));
        Assert.That(DndBardRules.SlotsAfterCast(1, 2), Is.EqualTo(1));
        Assert.That(DndBardRules.SlotsAfterCast(1, 0), Is.EqualTo(0));
    }

    [Test]
    public void LeatherArmorIsElevenPlusDexterity()
    {
        Assert.That(DndArmorRules.Score(DndArmorCategory.Light, 11, 3, false), Is.EqualTo(14));
        Assert.That(DndArmorRules.Score(DndArmorCategory.Light, 11, -1, false), Is.EqualTo(10));
        Assert.That(DndArmorRules.Score(DndArmorCategory.Light, 11, 2, true), Is.EqualTo(15));
        Assert.That(DndArmorRules.Score(DndArmorCategory.Medium, 13, 4, false), Is.EqualTo(15));
        Assert.That(DndArmorRules.Score(DndArmorCategory.Heavy, 16, 3, false), Is.EqualTo(16));
    }
}
