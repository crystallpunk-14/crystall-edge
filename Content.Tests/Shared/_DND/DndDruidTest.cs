using Content.Shared._DND.Classes;
using NUnit.Framework;

namespace Content.Tests.Shared._DND;

[TestFixture]
public sealed class DndDruidTest
{
    [Test]
    public void MagicianBonusIsAtLeastOne()
    {
        Assert.That(DndDruidRules.MagicianBonus(10), Is.EqualTo(1));
        Assert.That(DndDruidRules.MagicianBonus(8), Is.EqualTo(1));
        Assert.That(DndDruidRules.MagicianBonus(16), Is.EqualTo(3));
    }

    [Test]
    public void MagicianLearnsOneExtraCantrip()
    {
        Assert.That(DndDruidRules.CantripCount(2, DndPrimalOrders.Magician), Is.EqualTo(3));
        Assert.That(DndDruidRules.CantripCount(2, DndPrimalOrders.Warden), Is.EqualTo(2));
        Assert.That(DndDruidRules.SignDifficultyClass, Is.EqualTo(15));
    }
}
