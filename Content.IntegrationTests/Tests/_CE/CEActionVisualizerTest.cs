using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._CE.Actions.Components;
using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CEActionVisualizerTest : GameTest
{
    /// <summary>
    /// Every targeted CE action must show the player its cast radius and the shape of what it is aiming.
    /// </summary>
    [Test]
    public async Task TargetedActionsHaveVisualizers()
    {
        var pair = Pair;
        var server = pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var componentFactory = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            var protos = protoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => p.ID.StartsWith("CE"))
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => p.TryComp<TargetActionComponent>(out _, componentFactory))
                .OrderBy(p => p.ID)
                .ToList();

            Assert.Multiple(() =>
            {
                foreach (var proto in protos)
                {
                    Assert.That(proto.TryComp<CEVisualizeRadiusTargetActionComponent>(out _, componentFactory),
                        $"{proto.ID} is a targeted action without CEVisualizeRadiusTargetAction.");

                    var hasShape = proto.TryComp<CEVisualizeWideLineActionComponent>(out _, componentFactory) ||
                                   proto.TryComp<CEVisualizeConeActionComponent>(out _, componentFactory) ||
                                   proto.TryComp<CEVisualizeAoEZoneActionComponent>(out _, componentFactory) ||
                                   proto.TryComp<CEVisualizeTileActionComponent>(out _, componentFactory);

                    Assert.That(hasShape,
                        $"{proto.ID} is a targeted action without an aim shape visualizer (WideLine, Cone, AoEZone or Tile).");
                }
            });
        });
    }
}
