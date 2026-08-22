using Content.Server._CE.SpellMastery.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.InfusionAltar;
using Content.Shared._CE.InfusionAltar.Components;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.ResourceManager;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.SpellMastery.Components;
using Content.Shared._CE.SpellMastery.Prototypes;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.SpellMastery;

// CrystallEdge: rough copy of Content.Server._CE.InfusionAltar.CEInfusionAltarSystem.Pedestal, adapted
// so a buckled player takes the place of the catalyst item and grants a skill instead of spawning an
// item - not yet cleaned up/unified.
//
// Pedestal connections are resolved by scanning on every tick instead of via anchor-change event
// subscriptions like the infusion altar does, since CEInfusionAltarPedestalComponent's
// AnchorStateChangedEvent/EntityTerminatingEvent/ComponentRemove are already subscribed by
// CEInfusionAltarSystem - the engine does not allow subscribing the same component+event pair twice.

public sealed partial class CESpellMasterySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private CEMagicEssenceSystem _magicEssence = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery = default!;

    private const string PedestalSlot = "pedestal";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CESpellMasteryAltarComponent>();
        while (query.MoveNext(out var uid, out var altar))
        {
            if (_timing.CurTime < altar.NextCheckTime)
                continue;
            altar.NextCheckTime = _timing.CurTime + altar.CheckInterval;

            TickAltar((uid, altar), (float)altar.CheckInterval.TotalSeconds);
        }
    }

    /// <summary>
    /// Advances one ritual tick: grows/decays <see cref="CESpellMasteryAltarComponent.Instability"/> depending
    /// on the altar's current state, advances (or pauses) ritual progress, grants mastery on completion, and
    /// rolls for a mishap.
    /// </summary>
    private void TickAltar(Entity<CESpellMasteryAltarComponent> ent, float dt)
    {
        var altar = ent.Comp;

        altar.ConnectedPedestals = ResolveConnectedPedestals(ent);

        if (!this.IsPowered(ent.Owner, EntityManager))
        {
            altar.Instability = MathF.Max(0f, altar.Instability - altar.UnpoweredDecayRate * dt);
            altar.AttemptingRecipe = null;
            altar.RitualProgress = TimeSpan.Zero;

            // Losing power doesn't defuse whatever instability has already built up - it can still go
            // off while it drains back down, just without any chance of the ritual completing.
            var idleLevel = GetDangerLevel(altar);
            SpawnRandomMishup(ent, idleLevel);
            UpdateDangerVisuals(ent, idleLevel);
            return;
        }

        var player = GetBuckledPlayer(ent.Owner);

        if (player is null || !TryGetSingleton(out var singleton))
        {
            altar.Instability = MathF.Min(altar.MaxInstability, altar.Instability + altar.PoweredIdleInstabilityRate * dt);
            altar.AttemptingRecipe = null;
            altar.RitualProgress = TimeSpan.Zero;
        }
        else
        {
            var essenceInAltar = _magicEssence.GetEssence(ent.Owner, false);
            var recipe = ResolveAttemptedRecipe(ent,
                player.Value,
                singleton,
                essenceInAltar,
                out var cache,
                out var pedestalMatches,
                out var essenceSatisfied,
                out var pedestalsSatisfied);

            altar.AttemptingRecipe = recipe?.ID;

            if (recipe is null)
            {
                // Buckled but nothing they know matches a recipe - can never succeed. Treated the
                // same as broken conditions, except progress resets instead of pausing (there's no
                // attempt to resume).
                altar.Instability = MathF.Min(altar.MaxInstability,
                    altar.Instability + altar.BrokenConditionInstabilityRate * dt);
                altar.RitualProgress = TimeSpan.Zero;
            }
            else if (essenceSatisfied && pedestalsSatisfied)
            {
                altar.RitualProgress += TimeSpan.FromSeconds(dt);
                altar.Instability = MathF.Min(altar.MaxInstability,
                    altar.Instability + recipe.Instability * dt);

                if (altar.RitualProgress >= recipe.RitualDuration)
                {
                    Craft(ent, recipe, cache!, player.Value, pedestalMatches);
                    altar.Instability = 0f;
                    altar.RitualProgress = TimeSpan.Zero;
                    altar.AttemptingRecipe = null;
                }
            }
            else
            {
                // Conditions currently broken - progress is paused (not reset) until fixed.
                altar.Instability = MathF.Min(altar.MaxInstability,
                    altar.Instability + altar.BrokenConditionInstabilityRate * dt);
            }
        }

        var level = GetDangerLevel(altar);
        SpawnRandomMishup(ent, level);
        UpdateDangerVisuals(ent, level);
    }

    /// <summary>
    /// The single player buckled to the altar (like a bed), or null if nobody/more than one entity is
    /// buckled - takes the place of <c>ItemSlots.GetItemOrNull(altar, "catalyst")</c> in the infusion
    /// altar.
    /// </summary>
    private EntityUid? GetBuckledPlayer(EntityUid altarUid)
    {
        if (!TryComp<StrapComponent>(altarUid, out var strap) || strap.BuckledEntities.Count != 1)
            return null;

        foreach (var buckled in strap.BuckledEntities)
            return buckled;

        return null;
    }

    /// <summary>
    /// Scans <see cref="CESpellMasteryAltarComponent.PossiblePedestalsPositions"/> for anchored
    /// <see cref="CEInfusionAltarPedestalComponent"/> entities on every tick, instead of maintaining a
    /// live set via anchor-change events like the infusion altar does (see file header).
    /// </summary>
    private HashSet<EntityUid> ResolveConnectedPedestals(Entity<CESpellMasteryAltarComponent> altar)
    {
        var result = new HashSet<EntityUid>();

        if (!TryGetTile(altar.Owner, out var altarGrid, out var altarTile))
            return result;

        var query = EntityQueryEnumerator<CEInfusionAltarPedestalComponent, TransformComponent>();
        while (query.MoveNext(out var pedestalUid, out _, out var pedestalXform))
        {
            if (!pedestalXform.Anchored || pedestalXform.GridUid != altarGrid)
                continue;

            var pedestalTile = _mapSystem.TileIndicesFor(altarGrid, _mapGridQuery.Comp(altarGrid), pedestalXform.Coordinates);
            if (altar.Comp.PossiblePedestalsPositions.Contains(pedestalTile - altarTile))
                result.Add(pedestalUid);
        }

        return result;
    }

    /// <summary>
    /// Resolves the grid and tile indices an entity currently occupies, regardless of its anchor state.
    /// </summary>
    private bool TryGetTile(EntityUid uid, out EntityUid grid, out Vector2i tile)
    {
        grid = default;
        tile = default;

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !_mapGridQuery.TryComp(gridUid, out var gridComp))
            return false;

        grid = gridUid;
        tile = _mapSystem.TileIndicesFor(gridUid, gridComp, xform.Coordinates);
        return true;
    }

    /// <summary>
    /// Among recipes the buckled player hasn't already mastered, picks the one currently closest to
    /// completion (most satisfied essence types + pedestal requirements) - same scoring as the
    /// infusion altar, just without a catalyst item to narrow the candidates first.
    /// </summary>
    private CESpellMasteryRitualRecipePrototype? ResolveAttemptedRecipe(Entity<CESpellMasteryAltarComponent> altar,
        EntityUid player,
        CESpellMasterySingletonComponent singleton,
        Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> essenceLookup,
        out CESpellMasteryRecipeCache? cache,
        out List<(CEResourceRequirement Requirement, EntityUid Pedestal, EntityUid Item)> pedestalMatches,
        out bool essenceSatisfied,
        out bool pedestalsSatisfied)
    {
        cache = null;
        pedestalMatches = new List<(CEResourceRequirement, EntityUid, EntityUid)>();
        essenceSatisfied = false;
        pedestalsSatisfied = false;

        CESpellMasteryRitualRecipePrototype? best = null;
        var bestScore = -1;

        foreach (var recipe in _proto.EnumeratePrototypes<CESpellMasteryRitualRecipePrototype>())
        {
            // Already mastered - nothing left to train for this recipe. RequiredSkill is not
            // checked here - like the infusion altar, it's purely a guidebook reveal, never a gate
            // on actually performing the ritual (identified by essence/pedestal contents alone).
            if (_skill.HaveSkill(player, recipe.Result))
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
    /// Derives the altar's current danger level from its instability fraction. Calm never rolls
    /// mishaps; Unstable and Critical each have their own fixed per-tick chance and mishap pool
    /// (<see cref="SpawnRandomMishup"/>).
    /// </summary>
    private static CEInfusionAltarDangerLevel GetDangerLevel(CESpellMasteryAltarComponent altar)
    {
        var fraction = altar.Instability / altar.MaxInstability;
        return fraction switch
        {
            >= 0.66f => CEInfusionAltarDangerLevel.Critical,
            >= 0.33f => CEInfusionAltarDangerLevel.Unstable,
            _ => CEInfusionAltarDangerLevel.Calm,
        };
    }

    /// <summary>
    /// Rolls a fixed per-tick chance for a mishap depending on <paramref name="level"/> - 0% at Calm,
    /// <see cref="CESpellMasteryAltarComponent.UnstableMishapChance"/> at Unstable,
    /// <see cref="CESpellMasteryAltarComponent.CriticalMishapChance"/> at Critical. On success spawns a
    /// random entry from <see cref="CESpellMasteryAltarComponent.UnstableMishaps"/> (Unstable), or from
    /// both <see cref="CESpellMasteryAltarComponent.UnstableMishaps"/> and
    /// <see cref="CESpellMasteryAltarComponent.CriticalMishaps"/> combined (Critical). For now these
    /// lists point at the same mishap entity prototypes the infusion altar uses.
    /// </summary>
    private void SpawnRandomMishup(Entity<CESpellMasteryAltarComponent> ent, CEInfusionAltarDangerLevel level)
    {
        var altar = ent.Comp;

        var chance = level switch
        {
            CEInfusionAltarDangerLevel.Unstable => altar.UnstableMishapChance,
            CEInfusionAltarDangerLevel.Critical => altar.CriticalMishapChance,
            _ => 0f,
        };

        if (chance <= 0f || !_random.Prob(chance))
            return;

        var unstableCount = altar.UnstableMishaps.Count;
        var total = level == CEInfusionAltarDangerLevel.Critical
            ? unstableCount + altar.CriticalMishaps.Count
            : unstableCount;

        if (total == 0)
            return;

        var roll = _random.Next(total);
        var mishap = roll < unstableCount ? altar.UnstableMishaps[roll] : altar.CriticalMishaps[roll - unstableCount];

        Spawn(mishap, Transform(ent).Coordinates);
    }

    private void UpdateDangerVisuals(Entity<CESpellMasteryAltarComponent> ent, CEInfusionAltarDangerLevel level)
    {
        _appearance.SetData(ent.Owner, CEInfusionAltarDangerVisuals.Level, level);
        _appearance.SetData(ent.Owner, CEInfusionAltarDangerVisuals.Active, ent.Comp.Instability > 0f);
    }

    private static bool HasEnoughEssence(Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> available, CESpellMasteryRecipeCache cache)
    {
        foreach (var (type, amount) in cache.Essences)
        {
            if (!available.TryGetValue(type, out var have) || have < amount)
                return false;
        }

        return true;
    }

    private static int CountSatisfiedEssenceTypes(Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> available, CESpellMasteryRecipeCache cache)
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
    private int MatchPedestalRequirements(Entity<CESpellMasteryAltarComponent> altar,
        CESpellMasteryRecipeCache cache,
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

    /// <summary>
    /// Grants the recipe's Mastery skill to the buckled player and makes them a scientific-interest
    /// study subject - takes the place of the infusion altar's item spawn + item-tagged interest.
    /// </summary>
    private void Craft(Entity<CESpellMasteryAltarComponent> ent,
        CESpellMasteryRitualRecipePrototype recipe,
        CESpellMasteryRecipeCache cache,
        EntityUid player,
        List<(CEResourceRequirement Requirement, EntityUid Pedestal, EntityUid Item)> pedestalMatches)
    {
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

        _skill.TryAddSkill(player, recipe.Result, force: true);

        var interest = EnsureComp<CEScientificInterestComponent>(player);
        interest.StudiedBy.Clear();
        foreach (var (type, amount) in cache.Essences)
        {
            var points = (int)MathF.Ceiling(amount);
            if (points <= 0)
                continue;

            interest.Points[type] = interest.Points.GetValueOrDefault(type) + points;
        }

        Dirty(player, interest);
    }
}
