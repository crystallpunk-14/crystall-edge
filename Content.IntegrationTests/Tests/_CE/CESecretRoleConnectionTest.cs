#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._CE.Roles;
using Content.Server.GameTicking;
using Content.Shared._CE.Roles;
using Content.Shared.GameTicking;
using Content.Shared.Station.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
[TestOf(typeof(CESecretRoleSelectionSystem))]
public sealed class CESecretRoleConnectionTest : GameTest
{
    private const string RuleProtoId = "TSecretRoleConnectionRule";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {RuleProtoId}
  components:
  - type: GameRule
    minPlayers: 0
  - type: CESecretRoleSelection
    roles: []
";

    public override PoolSettings PoolSettings => new()
    {
        DummyTicker = false,
        Connected = true,
        InLobby = true,
        Dirty = true,
    };

    [Test]
    public async Task CheckEverySecretRoleLatejoin()
    {
        var pair = Pair;
        var server = pair.Server;
        var ticker = server.System<GameTicker>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var entMan = server.ResolveDependency<IEntityManager>();

        // Ready a dummy so the round can start without our own test client joining - we want them
        // to stay an observer so we can late-join them, once per role, below.
        var dummies = await server.AddDummySessions(1);
        await pair.RunTicksSync(5);

        await server.WaitPost(() => ticker.ToggleReady(dummies[0], true));
        await server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(Client.AttachedEntity, Is.Null, "Test client should not have auto-joined the round.");

        NetEntity stationNetEntity = default;
        await server.WaitAssertion(() =>
        {
            var station = entMan.EntityQuery<StationDataComponent>().First().Owner;
            stationNetEntity = entMan.GetNetEntity(station);
        });

        EntityUid ruleEnt = default;
        await server.WaitPost(() =>
        {
            ruleEnt = ticker.AddGameRule(RuleProtoId);
            ticker.StartGameRule(ruleEnt);
        });

        var secretRoles = server.System<CESecretRoleSelectionSystem>();
        var roleIds = protoMan.EnumeratePrototypes<CESecretRolePrototype>().Select(r => r.ID).ToList();
        Assert.That(roleIds, Is.Not.Empty, "No secret roles found to test.");

        foreach (var roleId in roleIds)
        {
            TestContext.WriteLine($"Testing role {roleId}");

            await server.WaitPost(() =>
            {
                Assert.That(Client.Session, Is.Not.Null);
                var session = server.PlayerMan.GetSessionById(Client.Session!.UserId);

                ticker.MakeJoinGame(session, entMan.GetEntity(stationNetEntity), "CEChef");
                Assert.That(secretRoles.TrySetSecretRole(session, roleId, out var error), error);
            });

            await pair.RunTicksSync(10);

            Assert.That(Client.AttachedEntity, Is.Not.Null, $"Client failed to late-join for role {roleId}");

            await server.WaitPost(() =>
            {
                if (Client.AttachedEntity is { } ent)
                    entMan.DeleteEntity(ent);
            });
            await pair.RunTicksSync(5);
        }
    }
}
