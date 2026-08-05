using System.Linq;
using Content.Server._CE.Science.Components;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Science;

public sealed partial class CEScienceSystem
{
    [Dependency] private IRobustRandom _random = default!;

    private void InitializePools(CEScienceComponent science)
    {
        science.AvailableDiscoveries.Clear();
        science.ChosenDiscoveries.Clear();

        foreach (var discovery in _proto.EnumeratePrototypes<CEScienceDiscoveryPrototype>())
        {
            science.AvailableDiscoveries.Add(discovery.ID);
        }
    }

    public List<ProtoId<CEScienceDiscoveryPrototype>> DrawOffer(
        CEScienceComponent science,
        ProtoId<CEScienceAreaPrototype> area,
        EntityUid actor,
        int count)
    {
        var drawn = new List<ProtoId<CEScienceDiscoveryPrototype>>();

        for (var i = 0; i < count; i++)
        {
            var candidates = GetAvailable(science, area, actor, drawn);
            if (candidates.Count == 0)
            {
                Refill(science, area);
                candidates = GetAvailable(science, area, actor, drawn);
            }

            if (candidates.Count == 0)
                break;

            var discovery = _random.Pick(candidates);
            science.AvailableDiscoveries.Remove(discovery);
            drawn.Add(discovery);
        }

        return drawn;
    }

    private List<ProtoId<CEScienceDiscoveryPrototype>> GetAvailable(
        CEScienceComponent science,
        ProtoId<CEScienceAreaPrototype> area,
        EntityUid actor,
        IReadOnlyCollection<ProtoId<CEScienceDiscoveryPrototype>> alreadyDrawn)
    {
        var result = new List<ProtoId<CEScienceDiscoveryPrototype>>();

        foreach (var id in science.AvailableDiscoveries)
        {
            if (alreadyDrawn.Contains(id) || !_proto.TryIndex(id, out var discovery) || discovery.Area != area)
                continue;

            var args = new CEEntityEffectArgs(EntityManager, actor, null, Angle.Zero, 0f, actor, null);
            if (discovery.Requirements.All(requirement => requirement.Passes(args)))
                result.Add(id);
        }

        return result;
    }

    private void Refill(CEScienceComponent science, ProtoId<CEScienceAreaPrototype> area)
    {
        foreach (var discovery in _proto.EnumeratePrototypes<CEScienceDiscoveryPrototype>())
        {
            if (discovery.Area != area || science.ChosenDiscoveries.Contains(discovery.ID))
                continue;

            science.AvailableDiscoveries.Add(discovery.ID);
        }
    }
}
