using System.Collections.Generic;
using Content.Shared._DND.Abilities;
using Content.Shared._DND.Backgrounds;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.Tests.Shared._DND;

[TestFixture]
public sealed class DndBackgroundScoresTest
{
    private static readonly ProtoId<DndAbilityPrototype>[] Acolyte =
    {
        DndAbilityPrototype.IntelligenceId,
        DndAbilityPrototype.WisdomId,
        DndAbilityPrototype.CharismaId,
    };

    [Test]
    public void AllThreeIncreasesEachListedAbilityByOne()
    {
        Assert.That(Bonus(DndAbilityPrototype.IntelligenceId, DndBackgroundIncrease.AllThree), Is.EqualTo(1));
        Assert.That(Bonus(DndAbilityPrototype.WisdomId, DndBackgroundIncrease.AllThree), Is.EqualTo(1));
        Assert.That(Bonus(DndAbilityPrototype.CharismaId, DndBackgroundIncrease.AllThree), Is.EqualTo(1));
        Assert.That(Bonus(DndAbilityPrototype.StrengthId, DndBackgroundIncrease.AllThree), Is.EqualTo(0));
    }

    [Test]
    public void SplitIncreasesTwoDifferentListedAbilities()
    {
        Assert.That(Bonus(
            DndAbilityPrototype.IntelligenceId,
            DndBackgroundIncrease.Split,
            DndAbilityPrototype.IntelligenceId,
            DndAbilityPrototype.WisdomId), Is.EqualTo(2));
        Assert.That(Bonus(
            DndAbilityPrototype.WisdomId,
            DndBackgroundIncrease.Split,
            DndAbilityPrototype.IntelligenceId,
            DndAbilityPrototype.WisdomId), Is.EqualTo(1));
        Assert.That(Bonus(
            DndAbilityPrototype.CharismaId,
            DndBackgroundIncrease.Split,
            DndAbilityPrototype.IntelligenceId,
            DndAbilityPrototype.WisdomId), Is.EqualTo(0));
    }

    [Test]
    public void IncreaseCannotRaiseAScoreAboveTwenty()
    {
        Assert.That(DndBackgroundScores.FinalScore(19, 2), Is.EqualTo(20));
        Assert.That(DndBackgroundScores.FinalScore(20, 1), Is.EqualTo(20));
        Assert.That(DndAbilityScores.Modifier(DndBackgroundScores.FinalScore(15, 2)), Is.EqualTo(3));
    }

    private static int Bonus(
        string abilityId,
        DndBackgroundIncrease increase,
        string plusTwo = DndAbilityPrototype.IntelligenceId,
        string plusOne = DndAbilityPrototype.WisdomId)
    {
        return DndBackgroundScores.Bonus(abilityId, Acolyte, increase, plusTwo, plusOne);
    }
}
