using Content.Server._CE.SpellMastery.Components;
using Content.Server.GameTicking.Events;
using Content.Shared._CE.Skill;
using Content.Shared._CE.SpellMastery.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.SpellMastery;

// CrystallEdge: rough copy of Content.Server._CE.InfusionAltar.CEInfusionAltarSystem, adapted so a
// buckled player takes the place of the catalyst item - not yet cleaned up/unified.

public sealed partial class CESpellMasterySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private CESharedSkillSystem _skill = default!;

    private readonly EntProtoId _singletonEntity = "CESpellMasterySingleton";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<CESpellMasterySingletonComponent, MapInitEvent>(OnMapInit);

        InitSkills();
        InitExamine();
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var uid = Spawn(_singletonEntity, MapCoordinates.Nullspace);

        if (TryComp<CESpellMasterySingletonComponent>(uid, out var singleton))
            RollAllRecipes(singleton);
    }

    private Entity<CESpellMasterySingletonComponent> EnsureSingleton()
    {
        var query = EntityQueryEnumerator<CESpellMasterySingletonComponent>();
        if (query.MoveNext(out var uid, out var comp))
            return (uid, comp);

        var newUid = Spawn(_singletonEntity, MapCoordinates.Nullspace);
        var newComp = Comp<CESpellMasterySingletonComponent>(newUid);
        RollAllRecipes(newComp);

        return (newUid, newComp);
    }

    private void RollAllRecipes(CESpellMasterySingletonComponent singleton)
    {
        foreach (var recipe in _proto.EnumeratePrototypes<CESpellMasteryRitualRecipePrototype>())
        {
            var cache = new CESpellMasteryRecipeCache();
            RollEssences(recipe, cache);
            RollPedestalRequirements(recipe, cache);
            singleton.Recipes[recipe.ID] = cache;
        }
    }

    private void OnMapInit(Entity<CESpellMasterySingletonComponent> ent, ref MapInitEvent args)
    {
        var query = EntityQueryEnumerator<CESpellMasterySingletonComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == ent.Owner)
                continue;

            QueueDel(ent.Owner);
            return;
        }
    }

    /// <summary>
    /// Rolls the recipe's total essence amount, then spends it point-by-point on a weighted random
    /// essence type from <see cref="CESpellMasteryRitualRecipePrototype.EssenceWeights"/>.
    /// </summary>
    private void RollEssences(CESpellMasteryRitualRecipePrototype recipe, CESpellMasteryRecipeCache cache)
    {
        // Safe exit: no essence types declared, nothing to roll, avoid _random.Next(0).
        if (recipe.EssenceWeights.Count == 0)
            return;

        var totalWeight = 0;
        foreach (var weight in recipe.EssenceWeights.Values)
        {
            totalWeight += weight;
        }

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
    /// Rolls <see cref="CESpellMasteryRitualRecipePrototype.PedestalCount"/> sub-pedestal requirements, each an
    /// independent weighted-random draw over the full <see cref="CESpellMasteryRitualRecipePrototype.PedestalRequirementPool"/>
    /// (with replacement, so entries can repeat).
    /// </summary>
    private void RollPedestalRequirements(CESpellMasteryRitualRecipePrototype recipe, CESpellMasteryRecipeCache cache)
    {
        // Safe exit: no candidates declared, nothing to roll, avoid _random.Next(0).
        if (recipe.PedestalRequirementPool.Count == 0)
            return;

        var totalWeight = 0;
        foreach (var entry in recipe.PedestalRequirementPool)
        {
            totalWeight += entry.Weight;
        }

        if (totalWeight <= 0)
            return;

        var count = recipe.PedestalCount.Next(_random);
        for (var i = 0; i < count; i++)
        {
            var roll = _random.Next(totalWeight);
            foreach (var entry in recipe.PedestalRequirementPool)
            {
                if (roll < entry.Weight)
                {
                    cache.PedestalRequirements.Add(entry.Requirement);
                    break;
                }

                roll -= entry.Weight;
            }
        }
    }

    /// <summary>
    /// Resolves the singleton spell mastery entity's data component via <see cref="EnsureSingleton"/>.
    /// </summary>
    public bool TryGetSingleton(out CESpellMasterySingletonComponent singleton)
    {
        singleton = EnsureSingleton().Comp;
        return true;
    }
}
