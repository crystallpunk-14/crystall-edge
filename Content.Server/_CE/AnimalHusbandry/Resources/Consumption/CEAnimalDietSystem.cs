using Content.Server.Nutrition.Components;
using Content.Shared._CE.Cooking.Components;
using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.AnimalHusbandry.Resources.Consumption;

/// <summary>
/// Optional prototype-authored diet policy for a consuming entity.
/// </summary>
[RegisterComponent]
public sealed partial class CEAnimalDietComponent : Component
{
    [DataField, AlwaysPushInheritance]
    public HashSet<ProtoId<CEFoodTagPrototype>> AllowedFoodTags = new();

    [DataField, AlwaysPushInheritance]
    public HashSet<ProtoId<CEFoodTagPrototype>> ForbiddenFoodTags = new();
}

/// <summary>
/// Evaluates reusable animal diet policy after canonical vanilla food access,
/// mouth, solution and digestibility checks have passed.
/// </summary>
public sealed partial class CEAnimalDietSystem : EntitySystem
{
    [Dependency] private IngestionSystem _ingestion = default!;

    public bool CanSelectFood(EntityUid consumer, Entity<EdibleComponent?> food)
    {
        if (!Resolve(food.Owner, ref food.Comp, false))
            return false;

        if (_ingestion.GetEdibleType(food) != IngestionSystem.Food ||
            _ingestion.TotalNutrition(food) <= 0f ||
            !CanSelectEdible(consumer, food) ||
            !HasComp<IgnoreBadFoodComponent>(consumer) && HasComp<BadFoodComponent>(food.Owner))
            return false;

        if (!TryComp<CEAnimalDietComponent>(consumer, out var diet))
            return true;

        if (!TryComp<CEFoodTagComponent>(food.Owner, out var tags))
            return diet.AllowedFoodTags.Count == 0;

        var allowed = diet.AllowedFoodTags.Count == 0;
        foreach (var tag in tags.Tags)
        {
            if (diet.ForbiddenFoodTags.Contains(tag))
                return false;

            allowed |= diet.AllowedFoodTags.Contains(tag);
        }

        return allowed;
    }

    public bool CanSelectDrink(EntityUid consumer, Entity<EdibleComponent?> drink)
    {
        if (!Resolve(drink.Owner, ref drink.Comp, false))
            return false;

        return _ingestion.GetEdibleType(drink) == IngestionSystem.Drink &&
            _ingestion.TotalHydration(drink) > 0f &&
            CanSelectEdible(consumer, drink);
    }

    private bool CanSelectEdible(EntityUid consumer, Entity<EdibleComponent?> edible)
    {
        return _ingestion.CanIngest(consumer, edible.Owner) &&
            _ingestion.CanConsume(consumer, edible.Owner);
    }
}
