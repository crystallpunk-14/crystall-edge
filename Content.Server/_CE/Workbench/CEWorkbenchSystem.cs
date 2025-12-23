/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared._CE.Workbench;
using Content.Shared._CE.Workbench.Prototypes;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Workbench;

public sealed partial class CEWorkbenchSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    private EntityQuery<CEWorkbenchComponent> _workbenchQuery;

    public override void Initialize()
    {
        base.Initialize();
        InitProviders();
        InitAutoCrafter();
        InitUserCrafter();

        _workbenchQuery = GetEntityQuery<CEWorkbenchComponent>();

        SubscribeLocalEvent<CEWorkbenchComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEWorkbenchComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
    }

    private void OnMapInit(Entity<CEWorkbenchComponent> ent, ref MapInitEvent args)
    {
        foreach (var recipe in _proto.EnumeratePrototypes<CEWorkbenchRecipePrototype>())
        {
            if (ent.Comp.Recipes.Contains(recipe))
                continue;

            if (!ent.Comp.RecipeTags.Contains(recipe.Tag))
                continue;

            ent.Comp.Recipes.Add(recipe);
        }
    }

    private void OnBeforeUIOpen(Entity<CEWorkbenchComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUIRecipes((ent, ent.Comp));
    }

    private void UpdateUIRecipes(Entity<CEWorkbenchComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

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

    private bool CanCraftRecipe(CEWorkbenchRecipePrototype recipe, HashSet<EntityUid> entities, EntityUid user)
    {
        foreach (var req in recipe.Requirements)
        {
            if (!req.CheckRequirement(EntityManager, _proto, entities))
                return false;
        }

        return true;
    }
}
