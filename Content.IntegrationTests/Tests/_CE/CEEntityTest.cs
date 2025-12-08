#nullable enable
using System.Collections.Generic;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

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

                var ignoredProto = new HashSet<string> //This is vanilla chief engineer items that we dont wanna test
                {
                    "CEPDA",
                    "CEIDCard",
                };

                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.ID.StartsWith("CE"))
                        continue;

                    if (proto.Abstract || proto.HideSpawnMenu)
                        continue;

                    if (ignoredProto.Contains(proto.ID))
                        continue;

                    if (!proto.Categories.Contains(indexedFilter))
                        Assert.Fail($"EntityPrototype: {proto} is not marked abstract, or does not have a HideSpawnMenu or ForkFiltered category");
                }
            });
        });
        await pair.CleanReturnAsync();
    }
}
