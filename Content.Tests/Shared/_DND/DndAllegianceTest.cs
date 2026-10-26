using Content.Shared._DND.Factions;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.Tests.Shared._DND;

[TestFixture]
public sealed class DndAllegianceTest
{
    [Test]
    public void ASideDoesNotHarmItself()
    {
        Assert.That(DndAllegianceRules.Between(DndSides.Adventurers, DndSides.Adventurers), Is.EqualTo(DndStance.Party));
        Assert.That(DndAllegianceRules.Between(DndSides.Goblins, DndSides.Goblins), Is.EqualTo(DndStance.Party));
        Assert.That(DndAllegianceRules.Between(DndSides.Wolves, DndSides.Wolves), Is.EqualTo(DndStance.Party));
        Assert.That(DndAllegianceRules.CanHarm(DndSides.Adventurers, DndSides.Adventurers), Is.False);
        Assert.That(DndAllegianceRules.CanHarm(DndSides.Goblins, DndSides.Goblins), Is.False);
        Assert.That(DndAllegianceRules.CanHarm(DndSides.Wolves, DndSides.Wolves), Is.False);
    }

    [Test]
    public void DifferentSidesCanHarmEachOther()
    {
        Assert.That(DndAllegianceRules.Between(DndSides.Adventurers, DndSides.Goblins), Is.EqualTo(DndStance.Opposition));
        Assert.That(DndAllegianceRules.Between(DndSides.Adventurers, DndSides.Wolves), Is.EqualTo(DndStance.Opposition));
        Assert.That(DndAllegianceRules.Between(DndSides.Goblins, DndSides.Wolves), Is.EqualTo(DndStance.Opposition));
        Assert.That(DndAllegianceRules.CanHarm(DndSides.Adventurers, DndSides.Goblins), Is.True);
    }

    [Test]
    public void NeutralDoesNotStartAFightAndASwingStillLands()
    {
        Assert.That(DndAllegianceRules.Between(DndSides.Neutral, DndSides.Goblins), Is.EqualTo(DndStance.Neutral));
        Assert.That(DndAllegianceRules.Between(DndSides.Adventurers, DndSides.Neutral), Is.EqualTo(DndStance.Neutral));
        Assert.That(DndAllegianceRules.CanHarm(DndSides.Adventurers, DndSides.Neutral), Is.True);
    }

    [Test]
    public void AShiftJoinsTheOtherParty()
    {
        var current = DndAllegianceRules.Current(DndSides.Goblins, DndSides.Adventurers);
        Assert.That(DndAllegianceRules.Between(current, DndSides.Adventurers), Is.EqualTo(DndStance.Party));
        Assert.That(DndAllegianceRules.Between(current, DndSides.Goblins), Is.EqualTo(DndStance.Opposition));
        Assert.That(DndAllegianceRules.Between(DndAllegianceRules.Current(DndSides.Goblins, null), DndSides.Goblins), Is.EqualTo(DndStance.Party));
    }

    [Test]
    public void ACreatureWithoutASideCanStillBeHarmed()
    {
        ProtoId<DndSidePrototype>? none = null;
        Assert.That(DndAllegianceRules.Between(none, DndSides.Adventurers), Is.EqualTo(DndStance.Opposition));
        Assert.That(DndAllegianceRules.CanHarm(none, none), Is.True);
    }
}
