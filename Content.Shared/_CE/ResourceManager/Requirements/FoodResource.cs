using Content.Shared._CE.Cooking.Components;
using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.ResourceManager.Requirements;

[Serializable, NetSerializable]
public sealed partial class FoodResource : CEResourceRequirement
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

        var complexity = indexedRecipe.GetComplexity();

        return complexity * 6;
    }

    public override string GetRequirementAmount()
    {
        return $"{Count}u";
    }

    public override string GetRequirementTitle(IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Recipe, out var indexedRecipe))
            return "Unknown Recipe";

        return $"{Loc.GetString(indexedRecipe.FoodData.Name ?? "Unknown Food")} ({Count}u)";
    }

    public override List<CEResourceIconLayer> GetRequirementIcon(IEntityManager entManager, IPrototypeManager protoManager)
    {
        var layers = new List<CEResourceIconLayer>();
        if (!protoManager.TryIndex(Recipe, out var indexedRecipe))
            return layers;

        if (protoManager.TryIndex(indexedRecipe.FoodType, out var foodType) && foodType.IconHolder is { } holder)
            layers.Add(CEResourceIconLayer.FromEntity(holder));

        foreach (var visual in indexedRecipe.FoodData.Visuals)
        {
            if (visual.Visible == false)
                continue;

            SpriteSpecifier? sprite = null;
            if (visual.RsiPath is { } rsi && visual.State is { } state)
                sprite = new SpriteSpecifier.Rsi(new ResPath(rsi), state);
            else if (visual.TexturePath is { } texture)
                sprite = new SpriteSpecifier.Texture(new ResPath(texture));

            if (sprite is not null)
                layers.Add(CEResourceIconLayer.FromSprite(sprite, visual.Color));
        }

        return layers;
    }
}
