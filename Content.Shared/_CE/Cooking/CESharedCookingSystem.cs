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
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Cooking;

public abstract partial class CESharedCookingSystem : EntitySystem
{
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] protected SharedSolutionContainerSystem Solution = default!;
    [Dependency] protected SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ISerializationManager _serialization = default!;

    /// <summary>
    /// When overcooking food, half of the solution is removed and a quarter of it is added back as this reagent.
    /// </summary>
    private static readonly ProtoId<ReagentPrototype> BurntFoodReagent = "Ash";

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

        CacheAndOrderRecipes();
    }

    private void CacheAndOrderRecipes()
    {
        _orderedRecipes = _proto.EnumeratePrototypes<CECookingRecipePrototype>()
            .Where(recipe => recipe.Cookable)
            .OrderByDescending(recipe => recipe.GetComplexity())
            .ToList();
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<CECookingRecipePrototype>())
            return;

        CacheAndOrderRecipes();
    }

    [SubscribeLocalEvent]
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
    /// Transfer food data and solution from one food holder to another
    /// </summary>
    private bool TryTransferFood(Entity<CEFoodHolderComponent> target,
        Entity<CEFoodHolderComponent> source)
    {
        if (!source.Comp.CanGiveFood || !target.Comp.CanAcceptFood)
            return false;

        if (target.Comp.FoodType != source.Comp.FoodType)
            return false;

        if (source.Comp.FoodData is not { } sourceData)
            return false;

        if (!Solution.TryGetSolution(source.Owner, source.Comp.SolutionId, out var sourceSoln, out var sourceSolution))
            return false;

        if (!Solution.TryGetSolution(target.Owner, target.Comp.SolutionId, out var targetSoln, out var targetSolution))
            return false;

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

        var newData = CopyFoodData(sourceData);
        newData.Trash.Clear();

        //Trash
        //If we have a lot of trash, we put 1 random trash in each plate. If it's a last plate (out of solution in cooker), we put all the remaining trash in it.
        if (sourceSolution.Volume <= targetSolution.AvailableVolume)
        {
            newData.Trash.AddRange(sourceData.Trash);
            sourceData.Trash.Clear();
        }
        else if (sourceData.Trash.Count > 0 && _net.IsServer)
        {
            var newTrash = _random.Pick(sourceData.Trash);
            sourceData.Trash.Remove(newTrash);
            newData.Trash.Add(newTrash);
        }

        // Food data goes first: emptying the source clears its food data and drops its trash
        SetFoodData(target, newData);
        Dirty(source);

        Solution.TryTransferSolution(targetSoln.Value, sourceSolution, targetSolution.AvailableVolume);
        Solution.UpdateChemicals(sourceSoln.Value);

        return true;
    }

    private CEFoodData CopyFoodData(CEFoodData data)
    {
        return _serialization.CreateCopy(data, notNullableOverride: true);
    }

    private void SetFoodData(Entity<CEFoodHolderComponent> ent, CEFoodData? data)
    {
        ent.Comp.FoodData = data;
        UpdateFoodDataMeta(ent);
        Dirty(ent);
    }

    /// <summary>
    /// Removes the food data from the holder, dropping all the trash it still has.
    /// </summary>
    private void ClearFoodData(Entity<CEFoodHolderComponent> ent)
    {
        if (ent.Comp.FoodData is not { } data || TerminatingOrDeleted(ent))
            return;

        if (_net.IsServer)
        {
            foreach (var trash in data.Trash)
            {
                SpawnNextToOrDrop(trash, ent);
            }
        }

        SetFoodData(ent, null);
    }

    /// <summary>
    /// Applies name, description and flavors of the current food data, or restores the prototype ones if there is no food.
    /// </summary>
    private void UpdateFoodDataMeta(Entity<CEFoodHolderComponent> ent)
    {
        var data = ent.Comp.FoodData;
        var proto = MetaData(ent).EntityPrototype;

        //Name and Description
        if (ent.Comp.AutoRename)
        {
            var name = data?.Name is { } dataName ? Loc.GetString(dataName) : proto?.Name;
            var desc = data?.Desc is { } dataDesc ? Loc.GetString(dataDesc) : proto?.Description;

            if (name is not null)
                _metaData.SetEntityName(ent, name);
            if (desc is not null)
                _metaData.SetEntityDescription(ent, desc);
        }

        //Flavors
        var flavors = data?.Flavors;
        if (flavors is null &&
            proto is not null &&
            proto.TryGetComponent<FlavorProfileComponent>(out var protoFlavor, EntityManager.ComponentFactory))
            flavors = protoFlavor.Flavors;

        if (flavors is null)
        {
            RemComp<FlavorProfileComponent>(ent);
            return;
        }

        EnsureComp<FlavorProfileComponent>(ent, out var flavorComp);
        flavorComp.Flavors.Clear();
        flavorComp.Flavors.UnionWith(flavors);
    }

    public CECookingRecipePrototype? GetRecipe(Entity<CEFoodCookerComponent> ent, CEFoodHolderComponent holder)
    {
        if (!Container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return null;

        Solution.TryGetSolution(ent.Owner, holder.SolutionId, out _, out var solution);

        //Get all food tags from placed ingredients
        var allFoodTags = new List<ProtoId<CEFoodTagPrototype>>();
        foreach (var contained in container.ContainedEntities)
        {
            if (!TryComp<CEFoodTagComponent>(contained, out var foodTags))
                continue;

            allFoodTags.AddRange(foodTags.Tags);
        }

        return GetRecipe(holder.FoodType, solution, allFoodTags);
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

    private float GetRecipeComplexity(ProtoId<CECookingRecipePrototype>? recipe)
    {
        if (recipe is null)
            return 0;

        if (!_proto.Resolve(recipe.Value, out var indexedRecipe))
            return 0;

        return indexedRecipe.GetComplexity();
    }

    /// <summary>
    /// Combines all reagents and items inside the FoodHolder, adding a visual representation of food.
    /// </summary>
    private void Cook(Entity<CEFoodCookerComponent> ent, CEFoodHolderComponent holder, CECookingRecipePrototype recipe)
    {
        if (!Solution.TryGetSolution(ent.Owner, holder.SolutionId, out var soln, out var solution))
            return;

        if (!Container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        var newData = CopyFoodData(recipe.FoodData);

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
                newData.Flavors.UnionWith(flavorComp.Flavors);
            }

            QueueDel(contained);
        }

        if (solution.Volume <= 0)
            return;

        SetFoodData((ent, holder), newData);
    }

    /// <summary>
    /// Turns the cooked food into the burnt recipe of its food type and replaces part of the solution with ash.
    /// </summary>
    private void BurntFood(Entity<CEFoodCookerComponent> ent, CEFoodHolderComponent holder)
    {
        if (holder.FoodData is not { } oldData)
            return;

        if (!Solution.TryGetSolution(ent.Owner, holder.SolutionId, out var soln, out var solution))
            return;

        if (!_proto.Resolve(holder.FoodType, out var foodType) ||
            !_proto.Resolve(foodType.BurntRecipe, out var burntRecipe))
            return;

        var newData = CopyFoodData(burntRecipe.FoodData);
        newData.CurrentRecipe = burntRecipe.ID;
        newData.Trash.AddRange(oldData.Trash);
        newData.Flavors.UnionWith(oldData.Flavors);

        SetFoodData((ent, holder), newData);

        var replacedVolume = solution.Volume / 2;
        Solution.SplitSolution(soln.Value, replacedVolume);
        Solution.TryAddReagent(soln.Value, BurntFoodReagent, replacedVolume / 2);
    }
}
