using Content.Server._CE.InfusionAltar.Components;
using Content.Server.GameTicking.Events;
using Content.Shared._CE.InfusionAltar.Prototypes;
using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.InfusionAltar;

public sealed partial class CEInfusionAltarSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly EntProtoId _singletonEntity = "CEInfusionAltarSingleton";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<CEInfusionAltarSingletonComponent, MapInitEvent>(OnMapInit);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var uid = Spawn(_singletonEntity, MapCoordinates.Nullspace);

        if (!TryComp<CEInfusionAltarSingletonComponent>(uid, out var singleton))
            return;

        foreach (var recipe in _proto.EnumeratePrototypes<CEInfusionAltarRecipePrototype>())
        {
            singleton.Recipes[recipe.ID] = GenerateRecipe(recipe);
        }
    }

    private void OnMapInit(Entity<CEInfusionAltarSingletonComponent> ent, ref MapInitEvent args)
    {
        var query = EntityQueryEnumerator<CEInfusionAltarSingletonComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == ent.Owner)
                continue;

            QueueDel(ent.Owner);
            return;
        }
    }

    /// <summary>
    /// Rolls every round-random part of a recipe's requirements into a <see cref="CEInfusionAltarRecipeCache"/>.
    /// Currently just essence costs, but this is the single entry point future rolled requirements
    /// (surrounding ingredients, instability modifiers, etc.) should be added to as their own step here.
    /// </summary>
    public CEInfusionAltarRecipeCache GenerateRecipe(CEInfusionAltarRecipePrototype recipe)
    {
        var cache = new CEInfusionAltarRecipeCache();

        RollEssences(recipe, cache);

        return cache;
    }

    /// <summary>
    /// Rolls the recipe's total essence amount, then spends it point-by-point on a weighted random
    /// essence type from <see cref="CEInfusionAltarRecipePrototype.EssenceWeights"/>.
    /// </summary>
    private void RollEssences(CEInfusionAltarRecipePrototype recipe, CEInfusionAltarRecipeCache cache)
    {
        var totalWeight = 0;
        foreach (var weight in recipe.EssenceWeights.Values)
            totalWeight += weight;

        if (totalWeight <= 0)
            return;

        var amount = recipe.EssenceAmount.Next(_random);
        for (var i = 0; i < amount; i++)
        {
            var roll = _random.Next(totalWeight);
            foreach (var (type, weight) in recipe.EssenceWeights)
            {
                if (roll < weight)
                {
                    cache.Essences[type] = cache.Essences.GetValueOrDefault(type) + 1;
                    break;
                }

                roll -= weight;
            }
        }
    }

    /// <summary>
    /// Resolves the singleton infusion altar entity's data component, if it has been spawned this round.
    /// </summary>
    public bool TryGetSingleton(out CEInfusionAltarSingletonComponent singleton)
    {
        var query = EntityQueryEnumerator<CEInfusionAltarSingletonComponent>();
        if (query.MoveNext(out _, out var comp))
        {
            singleton = comp;
            return true;
        }

        singleton = default!;
        return false;
    }
}
