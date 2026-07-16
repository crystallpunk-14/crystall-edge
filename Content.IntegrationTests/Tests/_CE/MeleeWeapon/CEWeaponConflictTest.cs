#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._CE.Animation.Item.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE.MeleeWeapon;

[TestFixture]
public sealed class CEWeaponConflictTest : GameTest
{
    /// <summary>
    /// Verify across ALL prototypes that have CEWeaponComponent — none should also have
    /// the vanilla MeleeWeaponComponent. _CE weapons attack exclusively through CEWeaponSystem;
    /// a stray MeleeWeaponComponent would let vanilla melee logic run alongside it.
    /// </summary>
    [Test]
    public async Task AllCEWeaponPrototypesLackVanillaMeleeWeapon()
    {
        var pair = Pair;
        var server = Server;

        await server.WaitAssertion(() =>
        {
            foreach (var proto in SProtoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (pair.IsTestPrototype(proto))
                    continue;

                if (!proto.TryGetComponent<CEWeaponComponent>(out _, SEntMan.ComponentFactory))
                    continue;

                Assert.That(
                    proto.TryGetComponent<MeleeWeaponComponent>(out _, SEntMan.ComponentFactory),
                    Is.False,
                    $"Prototype '{proto.ID}' has both CEWeaponComponent and vanilla MeleeWeaponComponent");
            }
        });
    }
}
