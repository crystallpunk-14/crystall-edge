using Content.Server._CE.InfusionAltar.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.InfusionAltar.Components;
using Content.Shared._CE.InfusionAltar.Prototypes;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.ResourceManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.InfusionAltar;

public sealed partial class CEInfusionAltarSystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private CEMagicEssenceSystem _magicEssence = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    private const string CatalystSlot = "catalyst";
    private const string PedestalSlot = "pedestal";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEInfusionAltarComponent>();
        while (query.MoveNext(out var uid, out var altar))
        {
            if (_timing.CurTime < altar.NextCheckTime)
                continue;
            altar.NextCheckTime = _timing.CurTime + altar.CheckInterval;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            CheckPedestal((uid, altar));
        }
    }

    private void CheckPedestal(Entity<CEInfusionAltarComponent> ent)
    {
        if (_itemSlots.GetItemOrNull(ent.Owner, CatalystSlot) is not { } catalystEntity)
            return;

        var placedEntities = new HashSet<EntityUid> { catalystEntity };

        if (!TryGetSingleton(out var singleton))
            return;

        var essences = _magicEssence.GetEssence(ent.Owner, includeContents: false);
        var essenceLookup = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int>();
        foreach (var (type, amount) in essences)
        {
            essenceLookup[type] = amount;
        }

        foreach (var recipe in _proto.EnumeratePrototypes<CEInfusionAltarRecipePrototype>())
        {
            if (!recipe.Catalyst.CheckRequirement(EntityManager, _proto, placedEntities))
                continue;

            if (!singleton.Recipes.TryGetValue(recipe.ID, out var cache))
                continue;

            if (!HasEnoughEssence(essenceLookup, cache))
                continue;

            if (!TryMatchPedestalRequirements(ent, cache, out var pedestalMatches))
                continue;

            Craft(ent, recipe, cache, placedEntities, pedestalMatches);
            return;
        }
    }

    private static bool HasEnoughEssence(Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> available, CEInfusionAltarRecipeCache cache)
    {
        foreach (var (type, amount) in cache.Essences)
        {
            if (!available.TryGetValue(type, out var have) || have < amount)
                return false;
        }

        return true;
    }

    private bool TryMatchPedestalRequirements(Entity<CEInfusionAltarComponent> altar,
        CEInfusionAltarRecipeCache cache,
        out List<(CEResourceRequirement Requirement, EntityUid Item)> matches)
    {
        matches = new List<(CEResourceRequirement, EntityUid)>();

        if (cache.PedestalRequirements.Count == 0)
            return true;

        var available = new HashSet<EntityUid>(altar.Comp.ConnectedPedestals);

        foreach (var requirement in cache.PedestalRequirements)
        {
            EntityUid? matchedPedestal = null;
            var matchedItem = EntityUid.Invalid;

            foreach (var pedestalUid in available)
            {
                if (_itemSlots.GetItemOrNull(pedestalUid, PedestalSlot) is not { } item)
                    continue;

                var singleItemSet = new HashSet<EntityUid> { item };

                if (!requirement.CheckRequirement(EntityManager, _proto, singleItemSet))
                    continue;

                matchedPedestal = pedestalUid;
                matchedItem = item;
                break;
            }

            if (matchedPedestal is not { } pedestal)
                return false;

            available.Remove(pedestal);
            matches.Add((requirement, matchedItem));
        }

        return true;
    }

    private void Craft(Entity<CEInfusionAltarComponent> ent,
        CEInfusionAltarRecipePrototype recipe,
        CEInfusionAltarRecipeCache cache,
        HashSet<EntityUid> placedEntities,
        List<(CEResourceRequirement Requirement, EntityUid Item)> pedestalMatches)
    {
        recipe.Catalyst.PostCraft(EntityManager, _proto, placedEntities);

        foreach (var (requirement, item) in pedestalMatches)
        {
            var singleItemSet = new HashSet<EntityUid> { item };
            requirement.PostCraft(EntityManager, _proto, singleItemSet);
        }

        if (_solutionContainer.TryGetSolution((ent.Owner, null), ent.Comp.Solution, out var soln, out _))
        {
            foreach (var (type, amount) in cache.Essences)
            {
                if (!_proto.TryIndex(type, out var essenceType) || essenceType.Reagent is not { } reagent)
                    continue;

                _solutionContainer.RemoveReagent(soln.Value, reagent, amount);
            }
        }

        for (var i = 0; i < recipe.ResultCount; i++)
        {
            Spawn(recipe.Result, Transform(ent).Coordinates);
        }
    }
}
