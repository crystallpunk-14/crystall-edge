#nullable enable
using System.Collections.Generic;
using Content.Shared._CE.MagicEssence.Components;
using Content.Shared._CE.MagicFocus.Components;
using Content.Shared.Chemistry.Components;
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

                if (!proto.TryComp<ItemComponent>(out _, componentFactory))
                    continue;

                // Draw/inject vessels (droppers, syringes, vials) are meant to show only the
                // essence of whatever reagents were drawn into them, not their own material.
                if (proto.TryComp<InjectorComponent>(out _, componentFactory))
                    continue;

                // Explicitly opted out - e.g. a sealed vessel pre-filled with an essence reagent,
                // whose essence reading already comes entirely from that solution.
                if (proto.TryComp<CEMagicEssenceStructureExemptComponent>(out _, componentFactory))
                    continue;

                // Magic foci show their own stored essence, not a rolled material essence.
                if (proto.TryComp<CEMagicFocusComponent>(out _, componentFactory))
                    continue;

                if (!proto.TryComp<CEMagicEssenceStructureComponent>(out _, componentFactory))
                    missing.Add(proto.ID);
            }
        });

        Assert.That(missing, Is.Empty,
            $"{missing.Count} item EntityPrototype(s) have ItemComponent but no CEMagicEssenceStructureComponent:\n{string.Join('\n', missing)}");

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CheckDrawInjectVesselsHaveNoOwnEssenceStructure()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var componentFactory = server.ResolveDependency<IComponentFactory>();

        var polluted = new List<string>();

        await server.WaitAssertion(() =>
        {
            foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract)
                    continue;

                if (!proto.TryComp<InjectorComponent>(out _, componentFactory))
                    continue;

                if (proto.TryComp<CEMagicEssenceStructureComponent>(out _, componentFactory))
                    polluted.Add(proto.ID);
            }
        });

        Assert.That(polluted, Is.Empty,
            $"{polluted.Count} EntityPrototype(s) have InjectorComponent (draw/inject vessels) but also CEMagicEssenceStructureComponent, " +
            $"which pollutes the essence reading of whatever gets drawn into them with the vessel's own material essence:\n{string.Join('\n', polluted)}");

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CheckMagicFociHaveNoOwnEssenceStructure()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var componentFactory = server.ResolveDependency<IComponentFactory>();

        var polluted = new List<string>();

        await server.WaitAssertion(() =>
        {
            foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract)
                    continue;

                if (!proto.TryComp<CEMagicFocusComponent>(out _, componentFactory))
                    continue;

                if (proto.TryComp<CEMagicEssenceStructureComponent>(out _, componentFactory))
                    polluted.Add(proto.ID);
            }
        });

        Assert.That(polluted, Is.Empty,
            $"{polluted.Count} EntityPrototype(s) have CEMagicFocusComponent but also CEMagicEssenceStructureComponent, " +
            $"which pollutes the essence reading with a rolled material essence on top of the focus's own stored essence:\n{string.Join('\n', polluted)}");

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

                if (!proto.TryComp<CEMagicEssenceStructureComponent>(out var structure, componentFactory))
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
