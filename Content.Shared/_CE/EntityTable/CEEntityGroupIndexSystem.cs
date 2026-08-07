using Content.Shared._CE.EntityTable.Components;
using Content.Shared.Prototypes;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityTable;

/// <summary>
/// Indexes entity prototypes carrying <see cref="CEEntityGroupMemberComponent"/> by group key,
/// so <see cref="EntitySelectors.CEEntityGroupSelector"/> can pick a random member without
/// anyone having to maintain an explicit list of entities.
/// </summary>
public sealed partial class CEEntityGroupIndexSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IComponentFactory _factory = default!;

    private readonly Dictionary<string, Dictionary<EntProtoId, float>> _index = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        RebuildIndex();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        // Most reloads touch prototype kinds we don't care about (tags, recipes, etc) - skip those entirely.
        if (args.WasModified<EntityPrototype>())
            RebuildIndex();
    }

    private void RebuildIndex()
    {
        _index.Clear();

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract)
                continue;

            if (!proto.TryGetComponent(out CEEntityGroupMemberComponent? member, _factory))
                continue;

            foreach (var (group, weight) in member.Groups)
            {
                if (!_index.TryGetValue(group, out var weights))
                    _index[group] = weights = new Dictionary<EntProtoId, float>();

                weights[proto.ID] = weight;
            }
        }
    }

    /// <summary>
    /// Picks a random entity prototype registered under <paramref name="group"/>, weighted by <see cref="CEEntityGroupMemberComponent.Groups"/>.
    /// </summary>
    public bool TryPick(string group, System.Random rand, out EntProtoId result)
    {
        if (!_index.TryGetValue(group, out var weights) || weights.Count == 0)
        {
            Log.Error($"CEEntityGroupSelector: no entities registered for group [{group}]");
            result = default;
            return false;
        }

        result = SharedRandomExtensions.Pick(weights, rand);
        return true;
    }

    /// <summary>
    /// Enumerates every entity registered under <paramref name="group"/> with its weight, for listing/preview purposes.
    /// </summary>
    public IEnumerable<(EntProtoId Id, float Weight)> GetGroupEntries(string group)
    {
        if (!_index.TryGetValue(group, out var weights))
            yield break;

        foreach (var (id, weight) in weights)
        {
            yield return (id, weight);
        }
    }
}
