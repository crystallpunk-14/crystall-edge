using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._CE.Traits;
using Content.Shared._CE.Skill;
using Content.Shared._CE.Traits;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
[TestOf(typeof(CETraitSystem))]
[TestOf(typeof(CETraitRules))]
public sealed class CETraitTest : GameTest
{
    private const string Plus = "TTraitPlus";
    private const string Minus = "TTraitMinus";
    private const string Conflict = "TTraitConflict";
    private const string Locked = "TTraitLocked";
    private const string Missing = "TTraitMissing";

    private const string SkillPlus = "TTraitSkillPlus";
    private const string SkillMinus = "TTraitSkillMinus";
    private const string Gear = "TTraitGear";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: skill
  id: {SkillPlus}

- type: skill
  id: {SkillMinus}

- type: entity
  id: {Gear}
  components:
  - type: Item

- type: trait
  id: {Plus}
  skill: {SkillPlus}
  cost: 2
  gear: {Gear}

- type: trait
  id: {Minus}
  skill: {SkillMinus}
  cost: -2

- type: trait
  id: {Conflict}
  skill: {SkillMinus}
  cost: -2
  incompatible:
  - {Plus}

- type: trait
  id: {Locked}
  skill: {SkillMinus}
  requirements:
  - !type:CEAchievementRequirement
    achievement: BetaTester
";

    private static HumanoidCharacterProfile WithTraits(params string[] traits)
    {
        var profile = HumanoidCharacterProfile.Random();
        foreach (var trait in traits)
        {
            profile = profile.WithTraitPreference(trait, true);
        }

        return profile;
    }

    private static List<ProtoId<CETraitPrototype>> Ids(params string[] traits)
    {
        return traits.Select(t => new ProtoId<CETraitPrototype>(t)).ToList();
    }

    [Test]
    public async Task SelectionMustBeBalancedAndConflictFree()
    {
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(CETraitRules.GetBalance(Ids(Plus, Minus, Plus), SProtoMan), Is.EqualTo(2));

                Assert.That(CETraitRules.IsSelectionValid(Ids(), SProtoMan), Is.True, "Empty selection is balanced");
                Assert.That(CETraitRules.IsSelectionValid(Ids(Plus, Minus), SProtoMan), Is.True);
                Assert.That(CETraitRules.IsSelectionValid(Ids(Plus), SProtoMan), Is.False, "Positive balance");
                Assert.That(CETraitRules.IsSelectionValid(Ids(Minus), SProtoMan), Is.False, "Negative balance");
                Assert.That(CETraitRules.IsSelectionValid(Ids(Plus, Conflict), SProtoMan), Is.False, "Balanced but conflicting");
                Assert.That(CETraitRules.IsSelectionValid(Ids(Missing), SProtoMan), Is.False, "Unknown trait");

                Assert.That(CETraitRules.AreIncompatible(Plus, Conflict, SProtoMan), Is.True, "Conflict is symmetric");
                Assert.That(CETraitRules.AreIncompatible(Conflict, Plus, SProtoMan), Is.True);
                Assert.That(CETraitRules.AreIncompatible(Plus, Minus, SProtoMan), Is.False);
            });
        });
    }

    [Test]
    public async Task EnsureValidDropsUnknownAndResetsInvalidSelection()
    {
        var server = Pair.Server;
        var session = (await server.AddDummySessions(1))[0];

        await server.WaitAssertion(() =>
        {
            var valid = WithTraits(Plus, Minus, Missing);
            valid.EnsureValid(session, server.InstanceDependencyCollection);
            Assert.That(valid.TraitPreferences, Is.EquivalentTo(Ids(Plus, Minus)), "Unknown trait is dropped, the rest kept");

            var unbalanced = WithTraits(Plus);
            unbalanced.EnsureValid(session, server.InstanceDependencyCollection);
            Assert.That(unbalanced.TraitPreferences, Is.Empty, "Unbalanced selection is reset");

            var conflicting = WithTraits(Plus, Conflict);
            conflicting.EnsureValid(session, server.InstanceDependencyCollection);
            Assert.That(conflicting.TraitPreferences, Is.Empty, "Conflicting selection is reset");
        });
    }

    [Test]
    public async Task InvalidOrLockedSelectionGrantsNothing()
    {
        var server = Pair.Server;
        var traits = SEntMan.System<CETraitSystem>();
        var session = (await server.AddDummySessions(1))[0];

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(traits.CanReceiveTraits(session, WithTraits(Plus, Minus)), Is.True);
                Assert.That(traits.CanReceiveTraits(session, WithTraits()), Is.True);
                Assert.That(traits.CanReceiveTraits(session, WithTraits(Plus)), Is.False, "Unbalanced");
                Assert.That(traits.CanReceiveTraits(session, WithTraits(Plus, Conflict)), Is.False, "Conflicting");
                Assert.That(traits.CanReceiveTraits(session, WithTraits(Locked)), Is.False, "Locked behind an achievement");
            });
        });
    }

    [Test]
    public async Task EachTraitRollsIndependentlyAtHalfChance()
    {
        const int rolls = 1000;
        var traits = SEntMan.System<CETraitSystem>();

        await Pair.Server.WaitAssertion(() =>
        {
            var plus = 0;
            var minus = 0;
            var both = 0;
            var none = 0;

            for (var i = 0; i < rolls; i++)
            {
                var rolled = traits.RollTraits(Ids(Plus, Minus));
                var hasPlus = rolled.Contains(Plus);
                var hasMinus = rolled.Contains(Minus);

                if (hasPlus)
                    plus++;
                if (hasMinus)
                    minus++;
                if (hasPlus && hasMinus)
                    both++;
                if (!hasPlus && !hasMinus)
                    none++;
            }

            // ~500 expected, the bounds are over 9 sigma away.
            Assert.Multiple(() =>
            {
                Assert.That(plus, Is.InRange(350, 650));
                Assert.That(minus, Is.InRange(350, 650));
                Assert.That(both, Is.InRange(150, 350), "Traits roll independently");
                Assert.That(none, Is.InRange(150, 350), "Traits roll independently");
            });
        });
    }

    [Test]
    public async Task AppliedTraitGrantsBadgedSkillAndGear()
    {
        var pair = Pair;
        var server = pair.Server;
        var traits = SEntMan.System<CETraitSystem>();
        var skills = SEntMan.System<CESharedSkillSystem>();
        var hands = SEntMan.System<SharedHandsSystem>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var mob = SEntMan.SpawnEntity("CEMobHuman", map.GridCoords);

            traits.ApplyTrait(mob, Plus);

            Assert.Multiple(() =>
            {
                Assert.That(skills.HaveSkill(mob, SkillPlus), Is.True);
                Assert.That(skills.GetDescriptor(mob, SkillPlus)?.Name,Is.EqualTo(CETraitSystem.BadgeName));
                Assert.That(hands.EnumerateHeld(mob).Any(e => SEntMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID == Gear),
                    Is.True,
                    "Trait gear is put into hands");
            });
        });
    }
}
