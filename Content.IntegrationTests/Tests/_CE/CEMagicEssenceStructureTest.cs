#nullable enable
using System.Collections.Generic;
using Content.Shared._CE.MagicEssence.Components;
using Content.Shared.Item;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CEMagicEssenceStructureTest
{
    // Vanilla prototypes that happen to start with "CE" (e.g. Chief Engineer's PDA/ID) but aren't
    // actually CrystallEdge content, so they shouldn't be expected to carry thaumaturgical essence.
    private static readonly HashSet<string> IgnoredProto = new()
    {
        "CEPDA",
        "CEIDCard",
    };

    [Test]
    public async Task CheckAllCEItemsHaveMagicEssenceStructure()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var componentFactory = server.ResolveDependency<IComponentFactory>();

        var missing = new List<string>();

        await server.WaitAssertion(() =>
        {
            foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.ID.StartsWith("CE"))
                    continue;

                if (proto.Abstract || proto.HideSpawnMenu)
                    continue;

                if (IgnoredProto.Contains(proto.ID))
                    continue;

                if (!proto.TryGetComponent<ItemComponent>(out _, componentFactory))
                    continue;

                if (!proto.TryGetComponent<CEMagicEssenceStructureComponent>(out _, componentFactory))
                    missing.Add(proto.ID);
            }
        });

        Assert.That(missing, Is.Empty,
            $"{missing.Count} item EntityPrototype(s) have ItemComponent but no CEMagicEssenceStructureComponent:\n{string.Join('\n', missing)}");

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CheckMagicEssenceStructureCanNeverRollEmpty()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var componentFactory = server.ResolveDependency<IComponentFactory>();

        var guaranteedEmpty = new List<string>();

        await server.WaitAssertion(() =>
        {
            foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract)
                    continue;

                if (!proto.TryGetComponent<CEMagicEssenceStructureComponent>(out var structure, componentFactory))
                    continue;

                var hasGuaranteedEssence = false;
                foreach (var minMax in structure.Essences.Values)
                {
                    if (minMax.Min < 1)
                        continue;

                    hasGuaranteedEssence = true;
                    break;
                }

                if (!hasGuaranteedEssence)
                    guaranteedEmpty.Add(proto.ID);
            }
        });

        Assert.That(guaranteedEmpty, Is.Empty,
            $"{guaranteedEmpty.Count} EntityPrototype(s) with CEMagicEssenceStructureComponent have no essence with a minimum of at least 1, " +
            $"meaning they could roll with zero essences at all:\n{string.Join('\n', guaranteedEmpty)}");

        await pair.CleanReturnAsync();
    }
}
