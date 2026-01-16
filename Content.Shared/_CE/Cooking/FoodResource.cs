using System.Linq;
using Content.Shared._CE.Cooking.Components;
using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared._CE.Workbench;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Cooking;

public sealed partial class FoodResource : CEWorkbenchCraftRequirement
{
    [DataField(required: true)]
    public ProtoId<CECookingRecipePrototype> Recipe;

    [DataField]
    public FixedPoint2 Count = 1;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        var solutionSys = entManager.System<SharedSolutionContainerSystem>();
        foreach (var ent in placedEntities)
        {
            if (!entManager.TryGetComponent<CEFoodHolderComponent>(ent, out var foodHolder))
                continue;

            if (!entManager.HasComponent<EdibleComponent>(ent))
                continue;

            if (foodHolder.FoodData?.CurrentRecipe != Recipe)
                continue;

            if (!solutionSys.TryGetSolution(ent, foodHolder.SolutionId, out _, out var solution))
                continue;

            if (solution.Volume < Count)
                continue;

            return true;
        }

        return false;
    }

    public override void PostCraft(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        var solutionSys = entManager.System<SharedSolutionContainerSystem>();

        foreach (var ent in placedEntities)
        {
            if (!entManager.TryGetComponent<CEFoodHolderComponent>(ent, out var foodHolder))
                continue;

            if (!entManager.HasComponent<EdibleComponent>(ent))
                continue;

            if (foodHolder.FoodData?.CurrentRecipe != Recipe)
                continue;

            if (!solutionSys.TryGetSolution(ent, foodHolder.SolutionId, out _, out var solution))
                continue;

            if (solution.Volume < Count)
                continue;

            entManager.DeleteEntity(ent);
            return;
        }
    }

    public override double GetPrice(IEntityManager entManager,
        IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Recipe, out var indexedRecipe))
            return 0;

        var complexity = indexedRecipe.Requirements.Sum(req => req.GetComplexity());

        return complexity * 6;
    }

    public override string GetRequirementTitle(IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Recipe, out var indexedRecipe))
            return "Unknown Recipe";

        return $"{Loc.GetString(indexedRecipe.FoodData.Name ?? "Unknown Food")} ({Count}u)";
    }

    public override SpriteSpecifier? GetRequirementTexture(IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Recipe, out var indexedRecipe))
            return null;

        var firstLayer = indexedRecipe.FoodData.Visuals.First();

        return new SpriteSpecifier.Rsi(new(firstLayer.RsiPath ?? ""), firstLayer.State ?? "");
    }

    public override Color GetRequirementColor(IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Recipe, out var indexedRecipe))
            return Color.White;

        var firstLayer = indexedRecipe.FoodData.Visuals.First();
        return firstLayer.Color ?? Color.White;
    }
}
