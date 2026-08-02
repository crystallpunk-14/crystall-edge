using Content.Server._CE.InfusionAltar.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.InfusionAltar;
using Content.Shared._CE.InfusionAltar.Components;
using Content.Shared._CE.InfusionAltar.Prototypes;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.ResourceManager;
using Content.Shared._CE.Science.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.InfusionAltar;

public sealed partial class CEInfusionAltarSystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private CEMagicEssenceSystem _magicEssence = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private const string CatalystSlot = "catalyst";
    private const string PedestalSlot = "pedestal";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEInfusionAltarComponent>();
        while (query.MoveNext(out var uid, out var altar))
        {
            if (_timing.CurTime >= altar.NextStabilizerScan)
            {
                altar.NextStabilizerScan = _timing.CurTime + altar.StabilizerScanInterval;
                ScanStabilizers((uid, altar));
            }

            if (_timing.CurTime < altar.NextCheckTime)
                continue;
            altar.NextCheckTime = _timing.CurTime + altar.CheckInterval;

            TickAltar((uid, altar), (float)altar.CheckInterval.TotalSeconds);
        }
    }

    /// <summary>
    /// Advances one ritual tick: grows/decays <see cref="CEInfusionAltarComponent.Instability"/> depending
    /// on the altar's current state, advances (or pauses) ritual progress, crafts on completion, and rolls
    /// for a mishap.
    /// </summary>
    private void TickAltar(Entity<CEInfusionAltarComponent> ent, float dt)
    {
        var altar = ent.Comp;

        if (!this.IsPowered(ent.Owner, EntityManager))
        {
            altar.Instability = MathF.Max(0f, altar.Instability - altar.UnpoweredDecayRate * dt);
            altar.AttemptingRecipe = null;
            altar.RitualProgress = TimeSpan.Zero;

            // Losing power doesn't defuse whatever instability has already built up - it can still go
            // off while it drains back down, just without any chance of the ritual completing.
            RollMishap(ent);
            UpdateDangerVisuals(ent);
            return;
        }

        var catalystEntity = _itemSlots.GetItemOrNull(ent.Owner, CatalystSlot);

        if (catalystEntity is null || !TryGetSingleton(out var singleton))
        {
            altar.Instability = MathF.Min(altar.MaxInstability, altar.Instability + altar.PoweredIdleInstabilityRate * dt);
            altar.AttemptingRecipe = null;
            altar.RitualProgress = TimeSpan.Zero;
        }
        else
        {
            var essenceLookup = BuildEssenceLookup(ent.Owner);
            var recipe = ResolveAttemptedRecipe(ent,
                catalystEntity.Value,
                singleton,
                essenceLookup,
                out var cache,
                out var pedestalMatches,
                out var essenceSatisfied,
                out var pedestalsSatisfied);

            altar.AttemptingRecipe = recipe?.ID;

            if (recipe is null)
            {
                // Catalyst present but matches no known recipe - can never succeed.
                altar.Instability = MathF.Min(altar.MaxInstability, altar.Instability + altar.InvalidCatalystInstabilityRate * dt);
                altar.RitualProgress = TimeSpan.Zero;
            }
            else if (essenceSatisfied && pedestalsSatisfied)
            {
                altar.RitualProgress += TimeSpan.FromSeconds(dt);
                altar.Instability = MathF.Min(altar.MaxInstability,
                    altar.Instability + recipe.Instability * altar.StabilizerFactor * dt);

                if (altar.RitualProgress >= recipe.RitualDuration)
                {
                    Craft(ent, recipe, cache!, new HashSet<EntityUid> { catalystEntity.Value }, pedestalMatches);
                    altar.Instability = 0f;
                    altar.RitualProgress = TimeSpan.Zero;
                    altar.AttemptingRecipe = null;
                }
            }
            else
            {
                // Conditions currently broken - progress is paused (not reset) until fixed.
                altar.Instability = MathF.Min(altar.MaxInstability,
                    altar.Instability + altar.BrokenConditionInstabilityRate * altar.StabilizerFactor * dt);
            }
        }

        RollMishap(ent);
        UpdateDangerVisuals(ent);
    }

    /// <summary>
    /// Among recipes whose catalyst requirement matches the inserted item, picks the one currently
    /// closest to completion (most satisfied essence types + pedestal requirements). Catalysts are not
    /// guaranteed unique across recipes, so a naive first-match would misidentify the attempt.
    /// </summary>
    private CEInfusionAltarRecipePrototype? ResolveAttemptedRecipe(Entity<CEInfusionAltarComponent> altar,
        EntityUid catalystEntity,
        CEInfusionAltarSingletonComponent singleton,
        Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> essenceLookup,
        out CEInfusionAltarRecipeCache? cache,
        out List<(CEResourceRequirement Requirement, EntityUid Pedestal, EntityUid Item)> pedestalMatches,
        out bool essenceSatisfied,
        out bool pedestalsSatisfied)
    {
        cache = null;
        pedestalMatches = new List<(CEResourceRequirement, EntityUid, EntityUid)>();
        essenceSatisfied = false;
        pedestalsSatisfied = false;

        var placedEntities = new HashSet<EntityUid> { catalystEntity };

        CEInfusionAltarRecipePrototype? best = null;
        var bestScore = -1;

        foreach (var recipe in _proto.EnumeratePrototypes<CEInfusionAltarRecipePrototype>())
        {
            if (!recipe.Catalyst.CheckRequirement(EntityManager, _proto, placedEntities))
                continue;

            if (!singleton.Recipes.TryGetValue(recipe.ID, out var recipeCache))
                continue;

            var essenceOk = HasEnoughEssence(essenceLookup, recipeCache);
            var essenceScore = CountSatisfiedEssenceTypes(essenceLookup, recipeCache);

            var matchedCount = MatchPedestalRequirements(altar, recipeCache, out var matches);
            var pedestalOk = matchedCount == recipeCache.PedestalRequirements.Count;

            var score = essenceScore + matchedCount;

            if (score < bestScore)
                continue;
            if (score == bestScore && best is not null && string.CompareOrdinal(recipe.ID, best.ID) >= 0)
                continue;

            bestScore = score;
            best = recipe;
            cache = recipeCache;
            pedestalMatches = matches;
            essenceSatisfied = essenceOk;
            pedestalsSatisfied = pedestalOk;
        }

        return best;
    }

    /// <summary>
    /// Rolls <c>Instability / InstabilityDivisor</c> chance for a mishap; on success spawns a random entry
    /// from <see cref="CEInfusionAltarComponent.Mishaps"/> at the altar itself.
    /// </summary>
    private void RollMishap(Entity<CEInfusionAltarComponent> ent)
    {
        var altar = ent.Comp;
        if (altar.Mishaps.Count == 0 || altar.Instability <= 0f)
            return;

        var chance = MathF.Min(altar.Instability / altar.InstabilityDivisor, 1f);
        if (!_random.Prob(chance))
            return;

        var mishap = altar.Mishaps[_random.Next(altar.Mishaps.Count)];

        Spawn(mishap, Transform(ent).Coordinates);
    }

    private void UpdateDangerVisuals(Entity<CEInfusionAltarComponent> ent)
    {
        var fraction = ent.Comp.Instability / ent.Comp.MaxInstability;
        var level = fraction switch
        {
            >= 0.66f => CEInfusionAltarDangerLevel.Critical,
            >= 0.33f => CEInfusionAltarDangerLevel.Unstable,
            _ => CEInfusionAltarDangerLevel.Calm,
        };

        _appearance.SetData(ent.Owner, CEInfusionAltarDangerVisuals.Level, level);
    }

    private Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> BuildEssenceLookup(EntityUid altar)
    {
        var essences = _magicEssence.GetEssence(altar, includeContents: false);
        var essenceLookup = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int>();
        foreach (var (type, amount) in essences)
        {
            essenceLookup[type] = amount;
        }

        return essenceLookup;
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

    private static int CountSatisfiedEssenceTypes(Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> available, CEInfusionAltarRecipeCache cache)
    {
        var count = 0;
        foreach (var (type, amount) in cache.Essences)
        {
            if (available.TryGetValue(type, out var have) && have >= amount)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Greedily matches as many of the recipe's rolled pedestal requirements as possible against
    /// currently connected, powered pedestals. Unlike a strict all-or-nothing check, this keeps trying
    /// remaining requirements after a miss so the returned count can be used as a partial-match score.
    /// </summary>
    private int MatchPedestalRequirements(Entity<CEInfusionAltarComponent> altar,
        CEInfusionAltarRecipeCache cache,
        out List<(CEResourceRequirement Requirement, EntityUid Pedestal, EntityUid Item)> matches)
    {
        matches = new List<(CEResourceRequirement, EntityUid, EntityUid)>();

        if (cache.PedestalRequirements.Count == 0)
            return 0;

        var available = new HashSet<EntityUid>(altar.Comp.ConnectedPedestals);

        foreach (var requirement in cache.PedestalRequirements)
        {
            EntityUid? matchedPedestal = null;
            var matchedItem = EntityUid.Invalid;

            foreach (var pedestalUid in available)
            {
                if (!this.IsPowered(pedestalUid, EntityManager))
                    continue;

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
                continue;

            available.Remove(pedestal);
            matches.Add((requirement, pedestal, matchedItem));
        }

        return matches.Count;
    }

    private void Craft(Entity<CEInfusionAltarComponent> ent,
        CEInfusionAltarRecipePrototype recipe,
        CEInfusionAltarRecipeCache cache,
        HashSet<EntityUid> placedEntities,
        List<(CEResourceRequirement Requirement, EntityUid Pedestal, EntityUid Item)> pedestalMatches)
    {
        recipe.Catalyst.PostCraft(EntityManager, _proto, placedEntities);

        foreach (var (requirement, _, item) in pedestalMatches)
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
            var result = Spawn(recipe.Result, Transform(ent).Coordinates);
            GrantScientificInterest(result, cache);
        }
    }

    /// <summary>
    /// Makes a freshly crafted result studyable for research points (see <see cref="CEScientificInterestComponent"/>,
    /// same mechanic as ancient books), worth 10% of the essence spent crafting it - rounded up, per essence type.
    /// </summary>
    private void GrantScientificInterest(EntityUid result, CEInfusionAltarRecipeCache cache)
    {
        var interest = EnsureComp<CEScientificInterestComponent>(result);

        foreach (var (type, amount) in cache.Essences)
        {
            var points = (int)MathF.Ceiling(amount * 0.1f);
            if (points <= 0)
                continue;

            interest.Points[type] = interest.Points.GetValueOrDefault(type) + points;
        }

        Dirty(result, interest);
    }
}
