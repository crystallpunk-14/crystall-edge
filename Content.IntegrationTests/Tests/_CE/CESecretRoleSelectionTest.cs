using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server._CE.Roles;
using Content.Shared._CE.Roles;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
[TestOf(typeof(CESecretRoleSelectionSystem))]
public sealed class CESecretRoleSelectionTest : GameTest
{
    private const string RuleProtoId = "TSecretRoleRule";
    private const string Alpha = "TSecretAlpha";
    private const string Beta = "TSecretBeta";
    private const string Gamma = "TSecretGamma";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: secretRole
  id: {Alpha}

- type: secretRole
  id: {Beta}

- type: secretRole
  id: {Gamma}

- type: entity
  id: {RuleProtoId}
  components:
  - type: CESecretRoleSelection
    roles:
    - role: {Alpha}
      playerRatio: 10
      range: {{ min: 1, max: 1 }}
      weight: 5
    - role: {Beta}
      playerRatio: 10
      range: {{ min: 1, max: 1 }}
      weight: 5
    - role: {Gamma}
      playerRatio: 10
      range: {{ min: 1, max: 1 }}
      weight: 5
";

    // Reproduces a bug: with 3 players and 3 population-scaled roles wanting 1 each, the player who
    // set High on a specific role could end up with a different one, because per-role resolution let
    // a no-preference player claim it first.
    [Test]
    public async Task HighPriorityAlwaysWinsOverNoPreference()
    {
        var pair = Pair;
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var entMan = server.ResolveDependency<IEntityManager>();
        var secretRoles = entMan.System<CESecretRoleSelectionSystem>();

        var dummies = await server.AddDummySessions(3);

        await server.WaitAssertion(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                var ruleEnt = entMan.SpawnEntity(RuleProtoId, MapCoordinates.Nullspace);
                var ruleComp = entMan.GetComponent<CESecretRoleSelectionComponent>(ruleEnt);

                var profiles = new Dictionary<NetUserId, HumanoidCharacterProfile>
                {
                    [dummies[0].UserId] = HumanoidCharacterProfile.Random()
                        .WithSecretRolePriority(Gamma, JobPriority.High)
                        .WithSecretRolePriority(Alpha, JobPriority.Medium)
                        .WithSecretRolePriority(Beta, JobPriority.Medium),
                    [dummies[1].UserId] = HumanoidCharacterProfile.Random(),
                    [dummies[2].UserId] = HumanoidCharacterProfile.Random(),
                };

                var decisions = secretRoles.DecideSecretRoles((ruleEnt, ruleComp), dummies, profiles);

                Assert.That(decisions.GetValueOrDefault(dummies[0].UserId),
                    Is.EqualTo((ProtoId<CESecretRolePrototype>) Gamma),
                    $"High-priority pick lost to a no-preference player on iteration {i}");

                entMan.DeleteEntity(ruleEnt);
            }
        });
    }
}
