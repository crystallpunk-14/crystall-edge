using System.Linq;
using Content.Server._CE.InfusionAltar.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.InfusionAltar.Components;
using Content.Shared._CE.InfusionAltar.Prototypes;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Placeable;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.InfusionAltar;

public sealed partial class CEInfusionAltarSystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private CEMagicEssenceSystem _magicEssence = default!;
    
    [Dependency] private EntityQuery<ItemPlacerComponent> _itemPlacerQuery = default!;

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
        if (!_itemPlacerQuery.TryComp(ent.Owner, out var placer) || placer.PlacedEntities.Count != 1)
            return;

        var catalystEntity = placer.PlacedEntities.First();
        var placedEntities = new HashSet<EntityUid> { catalystEntity };

        if (!TryGetSingleton(out var singleton))
            return;

        var essences = _magicEssence.GetEssence(ent.Owner, includeContents: false);
        var essenceLookup = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int>();
        foreach (var (type, amount) in essences)
            essenceLookup[type] = amount;

        foreach (var recipe in _proto.EnumeratePrototypes<CEInfusionAltarRecipePrototype>())
        {
            // Cheapest check first: does the placed item even match this recipe's catalyst?
            if (!recipe.Catalyst.CheckRequirement(EntityManager, _proto, placedEntities))
                continue;

            if (!singleton.Recipes.TryGetValue(recipe.ID, out var cache))
                continue;

            if (!HasEnoughEssence(essenceLookup, cache))
                continue;

            Craft(ent, recipe, cache, placedEntities);
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

    private void Craft(Entity<CEInfusionAltarComponent> ent,
        CEInfusionAltarRecipePrototype recipe,
        CEInfusionAltarRecipeCache cache,
        HashSet<EntityUid> placedEntities)
    {
        recipe.Catalyst.PostCraft(EntityManager, _proto, placedEntities);

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