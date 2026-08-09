using System.Linq;
using Content.Client._CE.Farming;
using Content.IntegrationTests.Fixtures;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

/// <summary>
/// Tests that every entity prototype with CEPlantVisualsComponent has a grow-N sprite for
/// every declared growth step, and a grown-N sprite for every declared ready variation.
/// Catches mismatches like GrowthSteps being set higher than the number of sprites that
/// actually exist in the plant's RSI, which shows up in-game as an ERROR sprite once the
/// plant grows far enough to request a state that was never drawn.
/// </summary>
[TestFixture]
public sealed class CEPlantVisualsSpriteTest : GameTest
{
    [Test]
    public async Task PlantGrowthSpritesExist()
    {
        var pair = Pair;
        var client = pair.Client;
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var componentFactory = client.ResolveDependency<IComponentFactory>();
        var entMan = client.ResolveDependency<IEntityManager>();
        var spriteSys = client.System<SpriteSystem>();

        await client.WaitAssertion(() =>
        {
            // CEPlantVisualsComponent only exists on the client, so it must be looked up via the
            // client's own prototype manager - the server's prototype manager never parses it.
            // This must run on the client thread (inside WaitAssertion): EntityPrototype.TryGetComponent
            // resolves the global IoCManager internally, which has no context on the NUnit test thread.
            var protos = protoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => p.TryGetComponent<CEPlantVisualsComponent>(out _, componentFactory))
                .OrderBy(p => p.ID)
                .ToList();

            Assert.That(protos, Is.Not.Empty, "No entity prototypes with CEPlantVisualsComponent were found.");

            Assert.Multiple(() =>
            {
                foreach (var proto in protos)
                {
                    Assert.That(proto.TryGetComponent<CEPlantVisualsComponent>(out var visuals, componentFactory));

                    var uid = entMan.Spawn(proto.ID);

                    Assert.That(entMan.TryGetComponent(uid, out SpriteComponent sprite),
                        @$"{proto.ID} has CEPlantVisualsComponent but no SpriteComponent.");

                    if (spriteSys.LayerMapTryGet((uid, sprite), PlantVisualLayers.Base, out var baseLayerId, false))
                    {
                        Assert.That(spriteSys.TryGetLayer((uid, sprite), baseLayerId, out var baseLayer, false));
                        var rsi = baseLayer.ActualRsi;

                        for (var i = 1; i <= visuals!.GrowthSteps; i++)
                        {
                            var state = $"{visuals.GrowState}{i}";
                            Assert.That(rsi.TryGetState(state, out _),
                                @$"{proto.ID} has CEPlantVisualsComponent with GrowthSteps = {visuals.GrowthSteps}, but {rsi.Path} doesn't have state {state}!");
                        }

                        for (var i = 1; i <= visuals.ReadyVariations; i++)
                        {
                            var state = $"{visuals.ReadyState}{i}";
                            Assert.That(rsi.TryGetState(state, out _),
                                @$"{proto.ID} has CEPlantVisualsComponent with ReadyVariations = {visuals.ReadyVariations}, but {rsi.Path} doesn't have state {state}!");
                        }
                    }

                    if (visuals!.GrowUnshadedState != null &&
                        spriteSys.LayerMapTryGet((uid, sprite), PlantVisualLayers.BaseUnshaded, out var unshadedLayerId, false))
                    {
                        Assert.That(spriteSys.TryGetLayer((uid, sprite), unshadedLayerId, out var unshadedLayer, false));
                        var rsi = unshadedLayer.ActualRsi;

                        for (var i = 1; i <= visuals.GrowthSteps; i++)
                        {
                            var state = $"{visuals.GrowUnshadedState}{i}";
                            Assert.That(rsi.TryGetState(state, out _),
                                @$"{proto.ID} has CEPlantVisualsComponent with GrowthSteps = {visuals.GrowthSteps}, but {rsi.Path} doesn't have unshaded state {state}!");
                        }

                        if (visuals.ReadyUnshadedState != null)
                        {
                            for (var i = 1; i <= visuals.ReadyVariations; i++)
                            {
                                var state = $"{visuals.ReadyUnshadedState}{i}";
                                Assert.That(rsi.TryGetState(state, out _),
                                    @$"{proto.ID} has CEPlantVisualsComponent with ReadyVariations = {visuals.ReadyVariations}, but {rsi.Path} doesn't have unshaded state {state}!");
                            }
                        }
                    }

                    entMan.DeleteEntity(uid);
                }
            });
        });
    }
}
