using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server._CE.Roles;
using Content.Shared._CE.Roles;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
[TestOf(typeof(CESecretRoleSelectionSystem))]
public sealed class CESecretRoleSelectionTest : GameTest
{
    private const string RuleProtoId = "TSecretRoleRule";
    private const string MinPlayersRuleProtoId = "TSecretRoleMinPlayersRule";

    private const string SmallDepartment = "TSecretDepartmentSmall";
    private const string MediumDepartment = "TSecretDepartmentMedium";
    private const string LargeDepartment = "TSecretDepartmentLarge";

    private const string Alpha = "TSecretAlpha";
    private const string Beta = "TSecretBeta";
    private const string Gamma = "TSecretGamma";
    private const string Fallback = "TSecretFallback";

    private const int PlayerCount = 20;

    [TestPrototypes]
    private const string Prototypes = $@"
- type: secretRole
  id: {Alpha}

- type: secretRole
  id: {Beta}

- type: secretRole
  id: {Gamma}

- type: secretRole
  id: {Fallback}

- type: secretDepartment
  id: {SmallDepartment}
  name: ce-secret-department-city-name
  description: ce-secret-department-city-description
  color: ""#ffffff""
  roles:
  - {Alpha}

- type: secretDepartment
  id: {MediumDepartment}
  name: ce-secret-department-city-name
  description: ce-secret-department-city-description
  color: ""#ffffff""
  roles:
  - {Beta}

- type: secretDepartment
  id: {LargeDepartment}
  name: ce-secret-department-city-name
  description: ce-secret-department-city-description
  color: ""#ffffff""
  roles:
  - {Gamma}
  - {Fallback}

- type: entity
  id: {RuleProtoId}
  components:
  - type: CESecretRoleSelection
    departments:
    - department: {SmallDepartment}
      share: 0.10
      roles:
      - role: {Alpha}
    - department: {MediumDepartment}
      share: 0.20
      roles:
      - role: {Beta}
    - department: {LargeDepartment}
      share: 0.70
      roles:
      - role: {Gamma}
        max: 3
      - role: {Fallback}
        fallback: true

- type: entity
  id: {MinPlayersRuleProtoId}
  components:
  - type: CESecretRoleSelection
    departments:
    - department: {SmallDepartment}
      share: 0.10
      minPlayers: 100
      roles:
      - role: {Alpha}
    - department: {MediumDepartment}
      share: 0.20
      roles:
      - role: {Beta}
    - department: {LargeDepartment}
      share: 0.70
      roles:
      - role: {Gamma}
        max: 3
      - role: {Fallback}
        fallback: true
";

    /// <summary>
    /// Department sizes follow their shares exactly, role caps hold, and the fallback role only takes
    /// whatever its department has left once the other roles are capped.
    /// </summary>
    [Test]
    public async Task DepartmentSizesFollowShares()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var secretRoles = entMan.System<CESecretRoleSelectionSystem>();

        var dummies = await server.AddDummySessions(PlayerCount);

        await server.WaitAssertion(() =>
        {
            var ruleEnt = entMan.SpawnEntity(RuleProtoId, MapCoordinates.Nullspace);
            var ruleComp = entMan.GetComponent<CESecretRoleSelectionComponent>(ruleEnt);
            var profiles = MakeProfiles(dummies);

            for (var i = 0; i < 50; i++)
            {
                var counts = CountRoles(secretRoles.DecideSecretRoles((ruleEnt, ruleComp), dummies, profiles));

                Assert.Multiple(() =>
                {
                    Assert.That(counts.GetValueOrDefault(Alpha), Is.EqualTo(2), $"Small department size on iteration {i}");
                    Assert.That(counts.GetValueOrDefault(Beta), Is.EqualTo(4), $"Medium department size on iteration {i}");
                    Assert.That(counts.GetValueOrDefault(Gamma), Is.EqualTo(3), $"Capped role count on iteration {i}");
                    Assert.That(counts.GetValueOrDefault(Fallback), Is.EqualTo(11), $"Fallback role count on iteration {i}");
                });
            }

            entMan.DeleteEntity(ruleEnt);
        });
    }

    /// <summary>
    /// A department below its minPlayers gets no one, and everybody still receives a role.
    /// </summary>
    [Test]
    public async Task MinPlayersClosesDepartment()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var secretRoles = entMan.System<CESecretRoleSelectionSystem>();

        var dummies = await server.AddDummySessions(PlayerCount);

        await server.WaitAssertion(() =>
        {
            var ruleEnt = entMan.SpawnEntity(MinPlayersRuleProtoId, MapCoordinates.Nullspace);
            var ruleComp = entMan.GetComponent<CESecretRoleSelectionComponent>(ruleEnt);
            var profiles = MakeProfiles(dummies);

            for (var i = 0; i < 50; i++)
            {
                var decisions = secretRoles.DecideSecretRoles((ruleEnt, ruleComp), dummies, profiles);
                var counts = CountRoles(decisions);

                Assert.Multiple(() =>
                {
                    Assert.That(counts.GetValueOrDefault(Alpha), Is.Zero, $"Closed department got players on iteration {i}");
                    Assert.That(decisions, Has.Count.EqualTo(PlayerCount), $"Someone got no role on iteration {i}");
                });
            }

            entMan.DeleteEntity(ruleEnt);
        });
    }

    /// <summary>
    /// High priority on a role must noticeably raise the chance to land in its department - the small
    /// department holds 10% of the players, a High player should get it far more often.
    /// </summary>
    [Test]
    public async Task HighPriorityRaisesDepartmentChance()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var secretRoles = entMan.System<CESecretRoleSelectionSystem>();

        var dummies = await server.AddDummySessions(PlayerCount);

        await server.WaitAssertion(() =>
        {
            var ruleEnt = entMan.SpawnEntity(RuleProtoId, MapCoordinates.Nullspace);
            var ruleComp = entMan.GetComponent<CESecretRoleSelectionComponent>(ruleEnt);
            var profiles = MakeProfiles(dummies);
            profiles[dummies[0].UserId] = profiles[dummies[0].UserId].WithSecretRolePriority(Alpha, JobPriority.High);

            const int iterations = 200;
            var hits = 0;
            for (var i = 0; i < iterations; i++)
            {
                var decisions = secretRoles.DecideSecretRoles((ruleEnt, ruleComp), dummies, profiles);
                if (decisions.GetValueOrDefault(dummies[0].UserId) == Alpha)
                    hits++;
            }

            Assert.That(hits, Is.GreaterThan(iterations * 0.35), "High priority barely changed the department chance");

            entMan.DeleteEntity(ruleEnt);
        });
    }

    private static Dictionary<NetUserId, HumanoidCharacterProfile> MakeProfiles(ICommonSession[] sessions)
    {
        var profiles = new Dictionary<NetUserId, HumanoidCharacterProfile>();
        foreach (var session in sessions)
            profiles[session.UserId] = HumanoidCharacterProfile.Random();

        return profiles;
    }

    private static Dictionary<string, int> CountRoles(Dictionary<NetUserId, ProtoId<CESecretRolePrototype>> decisions)
    {
        var counts = new Dictionary<string, int>();
        foreach (var role in decisions.Values)
            counts[role.Id] = counts.GetValueOrDefault(role.Id) + 1;

        return counts;
    }

    /// <summary>
    /// <see cref="CESecretRolePrototype.GuaranteedJobs"/> must be unambiguous: a job may be guaranteed
    /// by at most one role, and every guaranteed job must exist.
    /// </summary>
    [Test]
    public async Task GuaranteedJobsAreUnambiguous()
    {
        var server = Pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var owners = new Dictionary<ProtoId<JobPrototype>, string>();
            foreach (var role in protoMan.EnumeratePrototypes<CESecretRolePrototype>())
            {
                foreach (var job in role.GuaranteedJobs)
                {
                    Assert.That(protoMan.HasIndex(job), $"Secret role {role.ID} guarantees unknown job {job}");
                    Assert.That(owners.TryAdd(job, role.ID),
                        $"Job {job} is guaranteed by both {owners.GetValueOrDefault(job)} and {role.ID}");
                }
            }
        });
    }
}
