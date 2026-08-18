using System.Linq;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Science;

public sealed partial class CEScienceSystem : CESharedScienceSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    public List<ProtoId<CEScienceDiscoveryPrototype>> DrawOffer(
        CEScienceResearchDataComponent data,
        ProtoId<CEScienceAreaPrototype> area,
        EntityUid actor,
        int count)
    {
        var drawn = new List<ProtoId<CEScienceDiscoveryPrototype>>();

        for (var i = 0; i < count; i++)
        {
            var candidates = GetAvailable(data, area, actor, drawn);
            if (candidates.Count == 0)
            {
                Refill(data, area, actor);
                candidates = GetAvailable(data, area, actor, drawn);
            }

            if (candidates.Count == 0)
                break;

            var discovery = _random.Pick(candidates);
            data.AvailableDiscoveries.Remove(discovery);
            drawn.Add(discovery);
        }

        return drawn;
    }

    /// <summary>
    /// Returns discoveries that were offered but not picked back into the actor's own pool, so they
    /// can come up again on a future offer instead of being lost forever.
    /// </summary>
    public void ReturnUnchosen(
        CEScienceResearchDataComponent data,
        IReadOnlyList<ProtoId<CEScienceDiscoveryPrototype>> candidates,
        ProtoId<CEScienceDiscoveryPrototype> chosen)
    {
        foreach (var candidate in candidates)
        {
            if (candidate != chosen)
                data.AvailableDiscoveries.Add(candidate);
        }
    }

    private List<ProtoId<CEScienceDiscoveryPrototype>> GetAvailable(
        CEScienceResearchDataComponent data,
        ProtoId<CEScienceAreaPrototype> area,
        EntityUid actor,
        IReadOnlyCollection<ProtoId<CEScienceDiscoveryPrototype>> alreadyDrawn)
    {
        var result = new List<ProtoId<CEScienceDiscoveryPrototype>>();

        foreach (var id in data.AvailableDiscoveries)
        {
            if (alreadyDrawn.Contains(id) || !_proto.TryIndex(id, out var discovery) || discovery.Area != area)
                continue;

            if (IsDiscoveryKnown(actor, discovery) || !DiscoveryRequirementsMet(actor, discovery))
                continue;

            result.Add(id);
        }

        return result;
    }

    private void Refill(CEScienceResearchDataComponent data, ProtoId<CEScienceAreaPrototype> area, EntityUid actor)
    {
        foreach (var discovery in _proto.EnumeratePrototypes<CEScienceDiscoveryPrototype>())
        {
            if (discovery.Abstract || discovery.Area != area || IsDiscoveryKnown(actor, discovery))
                continue;

            data.AvailableDiscoveries.Add(discovery.ID);
        }
    }
}
