using System.Linq;
using Content.Server._CE.Science.Components;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Science;

/// <summary>
/// The round-wide discovery pool: which discoveries are still available to be offered by a star,
/// versus already permanently placed by a player's choice. See
/// <see cref="Components.CEScienceComponent.AvailableDiscoveries"/>.
/// </summary>
public sealed partial class CEScienceSystem
{
    private const int OfferSize = 3;

    /// <summary>
    /// Seeds the round-wide discovery pool with every discovery prototype, available for drawing.
    /// </summary>
    private void InitializePools(CEScienceComponent science)
    {
        science.AvailableDiscoveries.Clear();
        science.ChosenDiscoveries.Clear();

        foreach (var discovery in _proto.EnumeratePrototypes<CEScienceDiscoveryPrototype>())
            science.AvailableDiscoveries.Add(discovery.ID);
    }

    /// <summary>
    /// Draws up to <see cref="OfferSize"/> random discoveries of the given area+rarity from the
    /// pool, removing them from <see cref="Components.CEScienceComponent.AvailableDiscoveries"/> so
    /// they can't be offered again elsewhere until resolved or refilled. Refills first if there
    /// aren't enough candidates left. May return fewer than <see cref="OfferSize"/> (even zero) if
    /// the area+rarity is nearly or fully exhausted.
    /// </summary>
    public List<ProtoId<CEScienceDiscoveryPrototype>> RollOffer(
        CEScienceComponent science,
        ProtoId<CEScienceAreaPrototype> area,
        ProtoId<CEScienceDiscoveryDifficultyPrototype> rarity)
    {
        var candidates = GetAvailable(science, area, rarity);
        if (candidates.Count < OfferSize)
        {
            Refill(science, area, rarity);
            candidates = GetAvailable(science, area, rarity);
        }

        var picked = new List<ProtoId<CEScienceDiscoveryPrototype>>();
        for (var i = 0; i < OfferSize && candidates.Count > 0; i++)
        {
            var pick = _random.PickAndTake(candidates);
            picked.Add(pick);
            science.AvailableDiscoveries.Remove(pick);
        }

        return picked;
    }

    /// <summary>
    /// Re-validates and applies a player's choice of one of an offered star's candidates: spends
    /// that candidate's own cost, teaches its knowledge, replaces the star cell with a concrete
    /// discovery cell, and permanently excludes it from future offers.
    /// </summary>
    public bool ResolveChoice(
        ProtoId<CEScienceAreaPrototype> area,
        Vector2i coordinate,
        ProtoId<CEScienceDiscoveryPrototype> discoveryId,
        EntityUid actor)
    {
        if (!TryGetSingleton(out var science)
            || !science.Areas.TryGetValue(area, out var areaCells)
            || !areaCells.TryGetValue(coordinate, out var cell)
            || cell is not CEScienceOfferedStarCell offered
            || !offered.Candidates.Contains(discoveryId))
        {
            return false;
        }

        if (!_proto.TryIndex(discoveryId, out var discovery))
            return false;

        var data = EnsureComp<CEScienceResearchDataComponent>(actor);

        if (!TrySpendPoints((actor, data), discovery.Cost))
            return false;

        // Replace the cell before teaching the knowledge - OnKnowledgeLearned reveals a 3x3 area
        // around whichever discovery cell matches the learned knowledge, and needs to find this
        // one already in place to do that.
        areaCells[coordinate] = new CEScienceDiscoveryCell(discoveryId);
        science.ChosenDiscoveries.Add(discoveryId);

        TryLearnDiscovery(actor, discoveryId);

        return true;
    }

    /// <summary>
    /// Every currently-available discovery matching the given area+rarity.
    /// </summary>
    private List<ProtoId<CEScienceDiscoveryPrototype>> GetAvailable(
        CEScienceComponent science,
        ProtoId<CEScienceAreaPrototype> area,
        ProtoId<CEScienceDiscoveryDifficultyPrototype> rarity)
    {
        return science.AvailableDiscoveries
            .Where(id => _proto.TryIndex(id, out var discovery) && discovery.Area == area && discovery.Rarity == rarity)
            .ToList();
    }

    /// <summary>
    /// Re-adds every discovery of this area+rarity back into the pool, except ones already
    /// permanently placed (<see cref="Components.CEScienceComponent.ChosenDiscoveries"/>) or
    /// currently sitting on some other unresolved offer on this area's map.
    /// </summary>
    private void Refill(CEScienceComponent science, ProtoId<CEScienceAreaPrototype> area, ProtoId<CEScienceDiscoveryDifficultyPrototype> rarity)
    {
        var inFlight = new HashSet<ProtoId<CEScienceDiscoveryPrototype>>();
        if (science.Areas.TryGetValue(area, out var areaCells))
        {
            foreach (var cell in areaCells.Values)
            {
                if (cell is CEScienceOfferedStarCell offered && offered.Rarity == rarity)
                    inFlight.UnionWith(offered.Candidates);
            }
        }

        foreach (var discovery in _proto.EnumeratePrototypes<CEScienceDiscoveryPrototype>())
        {
            if (discovery.Area != area || discovery.Rarity != rarity)
                continue;

            if (science.ChosenDiscoveries.Contains(discovery.ID) || inFlight.Contains(discovery.ID))
                continue;

            science.AvailableDiscoveries.Add(discovery.ID);
        }
    }
}
