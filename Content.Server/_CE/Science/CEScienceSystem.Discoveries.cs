using System.Linq;
using Content.Server._CE.Science.Components;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Science.Components;
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

    public bool TryGetNextDiscovery(
        CEScienceComponent science,
        ProtoId<CEScienceAreaPrototype> area,
        EntityUid actor,
        out ProtoId<CEScienceDiscoveryPrototype> discovery)
    {
        var candidates = GetAvailable(science, area, actor);
        if (candidates.Count == 0)
        {
            Refill(science, area);
            candidates = GetAvailable(science, area, actor);
        }

        if (candidates.Count == 0)
        {
            discovery = default;
            return false;
        }

        discovery = _random.Pick(candidates);
        science.AvailableDiscoveries.Remove(discovery);
        return true;
    }

    private List<ProtoId<CEScienceDiscoveryPrototype>> GetAvailable(
        CEScienceComponent science,
        ProtoId<CEScienceAreaPrototype> area,
        EntityUid actor)
    {
        var result = new List<ProtoId<CEScienceDiscoveryPrototype>>();

        foreach (var id in science.AvailableDiscoveries)
        {
            if (!_proto.TryIndex(id, out var discovery) || discovery.Area != area)
                continue;

            var args = new CEEntityEffectArgs(EntityManager, actor, null, Angle.Zero, 0f, actor, null);
            if (discovery.Requirements.All(requirement => requirement.Passes(args)))
                result.Add(id);
        }

        return result;
    }

    // Excludes discoveries already chosen for good, or currently sitting unresolved in someone
    // else's offer - both cases would otherwise let the same discovery get drawn twice at once.
    private void Refill(CEScienceComponent science, ProtoId<CEScienceAreaPrototype> area)
    {
        var reserved = new HashSet<ProtoId<CEScienceDiscoveryPrototype>>();
        var query = EntityQueryEnumerator<CEUnselectedDiscoveryProjectComponent>();
        while (query.MoveNext(out _, out var offer))
        {
            reserved.UnionWith(offer.Candidates);
        }

        foreach (var discovery in _proto.EnumeratePrototypes<CEScienceDiscoveryPrototype>())
        {
            if (discovery.Area != area)
                continue;

            if (science.ChosenDiscoveries.Contains(discovery.ID) || reserved.Contains(discovery.ID))
                continue;

            science.AvailableDiscoveries.Add(discovery.ID);
        }
    }
}
