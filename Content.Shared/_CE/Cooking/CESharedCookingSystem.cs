/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using Content.Shared._CE.Cooking.Components;
using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Fluids;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Cooking;

public abstract partial class CESharedCookingSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem Solution = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <summary>
    /// When overcooking food, we will replace the reagents inside with this reagent.
    /// </summary>
    private readonly ProtoId<ReagentPrototype> _burntFoodReagent = "Ash";

    /// <summary>
    /// Stores a list of all recipes sorted by complexity: the most complex ones at the beginning.
    /// When attempting to cook, the most complex recipes will be checked first,
    /// gradually moving down to the easiest ones.
    /// The easiest recipes are usually the most “abstract,”
    /// so they will be suitable for the largest number of recipes.
    /// </summary>
    private List<CECookingRecipePrototype> _orderedRecipes = [];

    public override void Initialize()
    {
        base.Initialize();
        InitTransfer();
        InitDoAfter();

        CacheAndOrderRecipes();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<CEFoodHolderComponent, ExaminedEvent>(OnExaminedEvent);
    }

    private void CacheAndOrderRecipes()
    {
        _orderedRecipes = _proto.EnumeratePrototypes<CECookingRecipePrototype>()
            .Where(recipe => recipe.Requirements.Count > 0) // Only include recipes with requirements
            .OrderByDescending(recipe => recipe.GetComplexity())
            .ToList();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<CECookingRecipePrototype>())
            return;

        CacheAndOrderRecipes();
    }

    private void OnExaminedEvent(Entity<CEFoodHolderComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.FoodData?.Name is null)
            return;

        if (!Solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out _, out var solution))
            return;

        if (solution.Volume == 0)
            return;

        var remaining = solution.Volume;

        args.PushMarkup(Loc.GetString("ce-cooking-examine",
            ("name", Loc.GetString(ent.Comp.FoodData.Name)),
            ("count", remaining)));
    }

    /// <summary>
    /// Transfer food data from cooker to holder
    /// </summary>
    private bool TryTransferFood(Entity<CEFoodHolderComponent> target,
        Entity<CEFoodHolderComponent> source)
    {
        if (!source.Comp.CanGiveFood || !target.Comp.CanAcceptFood)
            return false;

        if (target.Comp.FoodType != source.Comp.FoodType)
            return false;

        if (source.Comp.FoodData is null)
            return false;

        if (!TryComp<EdibleComponent>(target, out var holderFoodComp))
            return false;

        if (!Solution.TryGetSolution(source.Owner, source.Comp.SolutionId, out var sourceSoln, out var sourceSolution))
            return false;

        //Solutions
        if (Solution.TryGetSolution(target.Owner, holderFoodComp.Solution, out var targetSoln, out var targetSolution))
        {
            if (targetSolution.Volume > 0)
            {
                if (_net.IsServer)
                {
                    _popup.PopupEntity(
                        Loc.GetString("ce-cooking-popup-not-empty", ("name", MetaData(target).EntityName)),
                        target);
                }

                return false;
            }

            Solution.TryTransferSolution(targetSoln.Value, sourceSolution, targetSolution.MaxVolume);
        }

        //Trash
        //If we have a lot of trash, we put 1 random trash in each plate. If it's a last plate (out of solution in cooker), we put all the remaining trash in it.
        if (source.Comp.FoodData?.Trash.Count > 0)
        {
            if (sourceSolution.Volume <= 0)
                holderFoodComp.Trash.AddRange(source.Comp.FoodData.Trash);
            else
            {
                if (_net.IsServer)
                {
                    var newTrash = _random.Pick(source.Comp.FoodData.Trash);
                    source.Comp.FoodData.Trash.Remove(newTrash);
                    holderFoodComp.Trash.Add(newTrash);
                }
            }
        }

        if (source.Comp.FoodData is not null)
            SetFoodData(target, source.Comp.FoodData);

        Dirty(target);
        Dirty(source);

        Solution.UpdateChemicals(sourceSoln.Value);

        return true;
    }

    private void SetFoodData(Entity<CEFoodHolderComponent> ent, CEFoodData? data)
    {
        ent.Comp.FoodData = data is not null ? new CEFoodData(data) : null;
        UpdateFoodDataVisuals(ent);
    }

    protected void UpdateFoodDataVisuals(
        Entity<CEFoodHolderComponent> ent)
    {
        var data = ent.Comp.FoodData;

        if (data is null)
            return;

        //Name and Description
        if (ent.Comp.AutoRename)
        {
            if (data.Name is not null)
                _metaData.SetEntityName(ent, Loc.GetString(data.Name));
            if (data.Desc is not null)
                _metaData.SetEntityDescription(ent, Loc.GetString(data.Desc));
        }

        //Flavors
        EnsureComp<FlavorProfileComponent>(ent, out var flavorComp);
        flavorComp.Flavors.Clear();
        foreach (var flavor in data.Flavors)
        {
            flavorComp.Flavors.Add(flavor);
        }

        Dirty(ent);
    }

    public CECookingRecipePrototype? GetRecipe(Entity<CEFoodCookerComponent> ent)
    {
        if (!Container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return null;

        Solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out _, out var solution);

        //Get all food tags from placed ingredients
        var allFoodTags = new List<ProtoId<CEFoodTagPrototype>>();
        foreach (var contained in container.ContainedEntities)
        {
            if (!TryComp<CEFoodTagComponent>(contained, out var foodTags))
                continue;

            allFoodTags.AddRange(foodTags.Tags);
        }

        return GetRecipe(ent.Comp.FoodType, solution, allFoodTags);
    }

    public CECookingRecipePrototype? GetRecipe(ProtoId<CEFoodTypePrototype> foodType,
        Solution? solution,
        List<ProtoId<CEFoodTagPrototype>> allFoodTags)
    {
        if (_orderedRecipes.Count == 0)
        {
            throw new InvalidOperationException(
                "No cooking recipes found. Please ensure that the CECookingRecipePrototype is defined and loaded.");
        }

        CECookingRecipePrototype? selectedRecipe = null;
        foreach (var recipe in _orderedRecipes)
        {
            if (recipe.FoodType != foodType)
                continue;

            var conditionsMet = true;
            foreach (var condition in recipe.Requirements)
            {
                if (!condition.CheckRequirement(EntityManager, _proto, allFoodTags, solution))
                {
                    conditionsMet = false;
                    break;
                }
            }

            if (!conditionsMet)
                continue;

            selectedRecipe = recipe;
            break;
        }

        return selectedRecipe;
    }

    /// <summary>
    /// Combines all reagents and items inside the FoodHolder, adding a visual representation of food.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="recipe"></param>
    private void Cook(Entity<CEFoodCookerComponent> ent, CECookingRecipePrototype recipe)
    {
        if (!TryComp<CEFoodHolderComponent>(ent.Owner, out var holder))
            return;

        if (!Solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out var soln, out var solution))
            return;

        if (!Container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        var newData = new CEFoodData(recipe.FoodData);

        //Assign recipe to the FoodData
        newData.CurrentRecipe = recipe.ID;

        //Process entities
        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<EdibleComponent>(contained, out var food))
            {
                //Merge trash
                newData.Trash.AddRange(food.Trash);

                //Merge solutions
                if (Solution.TryGetSolution(contained, food.Solution, out _, out var foodSolution))
                {
                    Solution.TryMixAndOverflow(soln.Value, foodSolution, solution.MaxVolume, out var overflowed);
                    if (overflowed is not null)
                    {
                        _puddle.TrySplashSpillAt(ent, Transform(ent).Coordinates, overflowed, out _);
                    }
                }
            }

            if (TryComp<FlavorProfileComponent>(contained, out var flavorComp))
            {
                //Merge flavors
                foreach (var flavor in flavorComp.Flavors)
                {
                    newData.Flavors.Add(flavor);
                }
            }

            QueueDel(contained);
        }

        if (solution.Volume <= 0)
            return;

        SetFoodData((ent, holder), newData);
    }

    private void BurntFood(Entity<CEFoodCookerComponent> ent)
    {
        if (!TryComp<CEFoodHolderComponent>(ent, out var holder) || holder.FoodData is null)
            return;

        if (!Solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out var soln, out var solution))
            return;

        var replacedVolume = solution.Volume / 2;
        solution.SplitSolution(replacedVolume);
        solution.AddReagent(_burntFoodReagent, replacedVolume / 2);

        var newData = new CEFoodData(holder.FoodData);
        //Brown visual
        foreach (var visuals in newData.Visuals)
        {
            visuals.Color = Color.FromHex("#212121");
        }

        newData.Name = Loc.GetString("ce-meal-recipe-burned-trash-name");
        newData.Desc = Loc.GetString("ce-meal-recipe-burned-trash-desc");

        SetFoodData((ent, holder), newData);
    }
}
