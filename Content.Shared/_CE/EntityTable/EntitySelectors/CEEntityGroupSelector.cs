using System.Linq;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityTable.EntitySelectors;

/// <summary>
/// Picks a random entity prototype registered under <see cref="Group"/> via
/// <see cref="CEEntityGroupIndexSystem"/>, instead of listing candidates inline.
/// </summary>
public sealed partial class CEEntityGroupSelector : EntityTableSelector
{
    [DataField(required: true)]
    public string Group = string.Empty;

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        if (entMan.System<CEEntityGroupIndexSystem>().TryPick(Group, rand, out var picked))
            yield return picked;
    }

    protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        var entries = entMan.System<CEEntityGroupIndexSystem>().GetGroupEntries(Group).ToList();
        var totalWeight = entries.Sum(e => e.Weight);

        if (totalWeight <= 0)
            yield break;

        foreach (var (id, weight) in entries)
        {
            yield return (id, weight / totalWeight);
        }
    }

    protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return ListSpawnsImplementation(entMan, proto, ctx);
    }
}
