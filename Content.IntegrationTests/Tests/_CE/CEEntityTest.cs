using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

#nullable enable

[TestFixture]
public sealed class CEEntityTest
{
    [Test]
    public async Task CheckAllCEEntityHasForkFilteredCategory()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                if (!protoManager.TryIndex<EntityCategoryPrototype>("ForkFiltered", out var indexedFilter))
                    return;

                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.ID.StartsWith("CE"))
                        continue;

                    if (proto.Abstract || proto.HideSpawnMenu)
                        continue;

                    Assert.That(proto.Categories.Contains(indexedFilter), $"CE fork proto: {proto} does not marked abstract, or have a HideSpawnMenu or ForkFiltered category");
                }
            });
        });
        await pair.CleanReturnAsync();
    }
}
