#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._CE.Roles;
using Content.Server.GameTicking;
using Content.Shared._CE.Roles;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
[TestOf(typeof(CESecretRoleSelectionSystem))]
public sealed class CESecretRoleConnectionTest : GameTest
{
    private const string RuleProtoId = "TSecretRoleConnectionRule";
    private const string SheriffJob = "CESheriff";

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
            TestContext.Out.WriteLine($"Testing role {roleId}");

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

    /// <summary>
    /// A late joiner whose job is listed in some role's <see cref="CESecretRolePrototype.GuaranteedJobs"/>
    /// must receive exactly that role, even though the rule itself lists no roles to draw from.
    /// </summary>
    [Test]
    public async Task LateJoinGuaranteedJobGetsItsRole()
    {
        var pair = Pair;
        var server = pair.Server;
        var ticker = server.System<GameTicker>();
        var entMan = server.ResolveDependency<IEntityManager>();
        var secretRoles = server.System<CESecretRoleSelectionSystem>();
        var mindSystem = server.System<SharedMindSystem>();
        var roleSystem = server.System<SharedRoleSystem>();

        var dummies = await server.AddDummySessions(1);
        await pair.RunTicksSync(5);

        await server.WaitPost(() => ticker.ToggleReady(dummies[0], true));
        await server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

        await server.WaitPost(() =>
        {
            var ruleEnt = ticker.AddGameRule(RuleProtoId);
            ticker.StartGameRule(ruleEnt);
        });

        CESecretRolePrototype? expected = null;

        await server.WaitPost(() =>
        {
            Assert.That(secretRoles.TryGetGuaranteedSecretRole(SheriffJob, out expected),
                $"No secret role guarantees {SheriffJob}");

            var station = entMan.EntityQuery<StationDataComponent>().First().Owner;
            var session = server.PlayerMan.GetSessionById(Client.Session!.UserId);
            ticker.MakeJoinGame(session, station, SheriffJob);
        });

        await pair.RunTicksSync(10);

        await server.WaitAssertion(() =>
        {
            var session = server.PlayerMan.GetSessionById(Client.Session!.UserId);
            Assert.That(mindSystem.TryGetMind(session, out var mindId, out _), "Late joiner has no mind");
            Assert.That(roleSystem.MindHasRole<CESecretRoleComponent>(mindId, out var roleEnt),
                "Late joiner got no secret role");
            Assert.That(roleEnt!.Value.Comp2.Role, Is.EqualTo((ProtoId<CESecretRolePrototype>) expected!.ID));
        });
    }
}
