#nullable enable
using System.Collections.Generic;
using Content.Shared._CE.Medicine.Symptom;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CESymptomPrototypeTest
{
    /// <summary>
    /// Symptom sources are status effects, their symptoms are status effects, and no symptom is a source itself.
    /// </summary>
    [Test]
    public async Task SymptomSourcesAreValid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var compFactory = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            var sources = new Dictionary<string, CESymptomSourceComponent>();
            foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.TryGetComponent<CESymptomSourceComponent>(out var source, compFactory))
                    sources.Add(proto.ID, source);
            }

            Assert.Multiple(() =>
            {
                foreach (var (id, source) in sources)
                {
                    Assert.That(protoManager.Index<EntityPrototype>(id).TryGetComponent<StatusEffectComponent>(out _, compFactory),
                        $"{id} has {nameof(CESymptomSourceComponent)} but is not a status effect");

                    foreach (var symptom in source.Symptoms)
                    {
                        if (!protoManager.TryIndex(symptom, out var symptomProto))
                        {
                            Assert.Fail($"{id} lists unknown symptom {symptom}");
                            continue;
                        }

                        Assert.That(symptomProto.TryGetComponent<StatusEffectComponent>(out _, compFactory),
                            $"{id} lists symptom {symptom} which is not a status effect");

                        Assert.That(sources.ContainsKey(symptom),
                            Is.False,
                            $"{id} lists symptom {symptom} which has {nameof(CESymptomSourceComponent)} itself");
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
