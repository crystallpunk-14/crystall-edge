using System.Numerics;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared._CE.Workbench;
using Content.Shared._CE.Workbench.Prototypes;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

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
        SubscribeLocalEvent<CEWorkbenchComponent, CEWorkbenchUiClickRecipeMessage>(OnSetRecipe);
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

    private void OnSetRecipe(Entity<CEWorkbenchComponent> ent, ref CEWorkbenchUiClickRecipeMessage args)
    {
        if (!ent.Comp.Recipes.Contains(args.Recipe))
            return;

        ent.Comp.SelectedRecipe = args.Recipe;
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

        _userInterface.SetUiState(entity.Owner, CEWorkbenchUiKey.Key, new CEWorkbenchUiRecipesState(recipes, entity.Comp.SelectedRecipe));
    }

    private bool CanCraftRecipe(CEWorkbenchRecipePrototype recipe, HashSet<EntityUid> entities, EntityUid? user = null)
    {
        foreach (var req in recipe.Requirements)
        {
            if (!req.CheckRequirement(EntityManager, _proto, entities))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks recipe conditions and triggers failure effects.
    /// </summary>
    /// <returns>True if all conditions pass, otherwise false.</returns>
    private bool CheckRecipeConditions(CEWorkbenchRecipePrototype recipe, EntityUid workbench, EntityUid? user)
    {
        var passConditions = true;
        foreach (var condition in recipe.Conditions)
        {
            if (!condition.CheckCondition(EntityManager, _proto, workbench, user))
            {
                condition.FailedEffect(EntityManager, _proto, workbench, user);
                passConditions = false;
            }
            condition.PostCraft(EntityManager, _proto, workbench, user);
        }

        return passConditions;
    }

    /// <summary>
    /// Consumes resources required for crafting.
    /// </summary>
    private void ConsumeRecipeResources(CEWorkbenchRecipePrototype recipe, HashSet<EntityUid> resources)
    {
        foreach (var req in recipe.Requirements)
        {
            req.PostCraft(EntityManager, _proto, resources);
        }
    }

    /// <summary>
    /// Spawns the craft result and places it near the workbench.
    /// </summary>
    private void SpawnRecipeResult(CEWorkbenchRecipePrototype recipe, EntityUid workbench)
    {
        var resultEntities = new HashSet<EntityUid>();
        for (var i = 0; i < recipe.ResultCount; i++)
        {
            var resultEntity = Spawn(recipe.Result);
            resultEntities.Add(resultEntity);
        }

        // Teleport result to workbench AFTER crafting
        foreach (var resultEntity in resultEntities)
        {
            _transform.SetCoordinates(resultEntity, Transform(workbench).Coordinates.Offset(new Vector2(_random.NextFloat(-0.25f, 0.25f), _random.NextFloat(-0.25f, 0.25f))));
            _stack.TryMergeToContacts(resultEntity);
            _physics.WakeBody(resultEntity);
        }
    }
}
