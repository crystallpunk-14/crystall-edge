/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Workbench;
using Content.Shared.Placeable;

namespace Content.Server._CE.Workbench;

public sealed partial class CEWorkbenchSystem
{
    private void OnCraft(Entity<CEWorkbenchComponent> entity, ref CEWorkbenchUiCraftMessage args)
    {
        if (!entity.Comp.Recipes.Contains(args.Recipe))
            return;

        if (!_proto.Resolve(args.Recipe, out var prototype))
            return;

        StartCraft(entity, args.Actor, prototype);
    }

    private void UpdateUIRecipes(Entity<CEWorkbenchComponent> entity)
    {
        var getResource = new CEWorkbenchGetResourcesEvent();
        RaiseLocalEvent(entity, getResource);

        var resources = getResource.Resources;

        var recipes = new List<CEWorkbenchUiRecipesEntry>();
        foreach (var recipeId in entity.Comp.Recipes)
        {
            if (!_proto.Resolve(recipeId, out var indexedRecipe))
                continue;

            var canCraft = true;

            foreach (var requirement in indexedRecipe.Requirements)
            {
                if (!requirement.CheckRequirement(EntityManager, _proto, resources))
                {
                    canCraft = false;
                    break;
                }
            }

            var entry = new CEWorkbenchUiRecipesEntry(recipeId, canCraft);

            recipes.Add(entry);
        }

        _userInterface.SetUiState(entity.Owner, CEWorkbenchUiKey.Key, new CEWorkbenchUiRecipesState(recipes));
    }
}
