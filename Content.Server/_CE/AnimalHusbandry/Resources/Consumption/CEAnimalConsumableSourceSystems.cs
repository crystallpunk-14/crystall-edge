using Content.Server._CE.AnimalHusbandry.Infrastructure.Feeding;
using Content.Server._CE.Consumption;
using Content.Shared._CE.AnimalHusbandry.Resources.Consumption;
using Content.Shared._CE.Consumption;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityConditions;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.AnimalHusbandry.Resources.Consumption;

public sealed partial class CEFeedTroughConsumableSourceSystem
    : CENearestConsumableSourceSystem<CEFeedTroughConsumableSource, CEFeedTroughComponent>
{
    [Dependency] private CEAnimalFoodSystem _food = default!;

    protected override bool IsProviderValid(
        EntityUid consumer,
        Entity<CEFeedTroughComponent> provider,
        CEFeedTroughConsumableSource source)
    {
        return _food.CanProvideFood(provider, consumer);
    }

    protected override bool TryResolveConsumable(
        EntityUid consumer,
        EntityUid provider,
        CEFeedTroughConsumableSource source,
        out EntityUid consumable)
    {
        return _food.TryTakeFood(provider, consumer, out consumable);
    }

    protected override void ReleaseConsumable(
        EntityUid consumer,
        EntityUid provider,
        EntityUid consumable,
        bool consumed,
        CEFeedTroughConsumableSource source)
    {
        _food.ReleaseFood(provider, consumer, consumable);
    }
}

/// <summary>
/// Shared traversal and lifecycle for uncontained edible entities. Concrete
/// leaves own only the value policy used to accept a candidate.
/// </summary>
public abstract partial class CEWorldEdibleConsumableSourceSystem<TSource>
    : CENearestConsumableSourceSystem<TSource, EdibleComponent>
    where TSource : CEConsumableSourceBase<TSource>
{
    [Dependency] private IngestionSystem _ingestion = default!;

    protected abstract ProtoId<EdiblePrototype> EdibleType { get; }

    protected sealed override bool CanHandleProvider(EntityUid provider, TSource source)
    {
        return TryComp(provider, out EdibleComponent? edible) &&
            _ingestion.GetEdibleType((provider, edible)) == EdibleType;
    }

    protected sealed override bool IsProviderValid(
        EntityUid consumer,
        Entity<EdibleComponent> provider,
        TSource source)
    {
        return CanSelect(consumer, (provider.Owner, provider.Comp), source);
    }

    protected sealed override bool TryResolveConsumable(
        EntityUid consumer,
        EntityUid provider,
        TSource source,
        out EntityUid consumable)
    {
        consumable = default;
        if (!TryComp(provider, out EdibleComponent? edible) ||
            !CanSelect(consumer, (provider, edible), source))
            return false;

        consumable = provider;
        return true;
    }

    protected abstract bool CanSelect(
        EntityUid consumer,
        Entity<EdibleComponent?> edible,
        TSource source);
}

public sealed partial class CEWorldFoodConsumableSourceSystem
    : CEWorldEdibleConsumableSourceSystem<CEWorldFoodConsumableSource>
{
    [Dependency] private CEAnimalDietSystem _diet = default!;

    protected override ProtoId<EdiblePrototype> EdibleType => IngestionSystem.Food;

    protected override bool CanSelect(
        EntityUid consumer,
        Entity<EdibleComponent?> edible,
        CEWorldFoodConsumableSource source)
    {
        return _diet.CanSelectFood(consumer, edible);
    }
}

public sealed partial class CEWorldDrinkConsumableSourceSystem
    : CEWorldEdibleConsumableSourceSystem<CEWorldDrinkConsumableSource>
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private CEAnimalDietSystem _diet = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;

    protected override ProtoId<EdiblePrototype> EdibleType => IngestionSystem.Drink;

    protected override bool CanSelect(
        EntityUid consumer,
        Entity<EdibleComponent?> edible,
        CEWorldDrinkConsumableSource source)
    {
        return edible.Comp != null &&
            source.Conditions != null &&
            _diet.CanSelectDrink(consumer, edible) &&
            _solutions.TryGetSolution(edible.Owner, edible.Comp.Solution, out var solutionEntity, out _) &&
            _conditions.TryConditions(solutionEntity.Value.Owner, source.Conditions, consumer);
    }
}
