/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Numerics;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._CE.Workbench;
using Content.Shared._CE.Workbench.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Placeable;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Workbench;

public sealed partial class CEWorkbenchSystem : CESharedWorkbenchSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitProviders();

        SubscribeLocalEvent<CEWorkbenchComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<CEWorkbenchComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<CEWorkbenchComponent, ItemRemovedEvent>(OnItemRemoved);

        SubscribeLocalEvent<CEWorkbenchComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<CEWorkbenchComponent, CEWorkbenchUiCraftMessage>(OnCraft);

        SubscribeLocalEvent<CEWorkbenchComponent, CECraftDoAfterEvent>(OnCraftFinished);
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

    private void OnItemRemoved(Entity<CEWorkbenchComponent> ent, ref ItemRemovedEvent args)
    {
        UpdateUIRecipes(ent);
    }

    private void OnItemPlaced(Entity<CEWorkbenchComponent> ent, ref ItemPlacedEvent args)
    {
        UpdateUIRecipes(ent);
    }

    private void OnBeforeUIOpen(Entity<CEWorkbenchComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUIRecipes(ent);
    }

    private void OnCraftFinished(Entity<CEWorkbenchComponent> ent, ref CECraftDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!_proto.Resolve(args.Recipe, out var recipe))
            return;

        var getResource = new CEWorkbenchGetResourcesEvent();
        RaiseLocalEvent(ent.Owner, getResource);

        var resources = getResource.Resources;

        if (!CanCraftRecipe(recipe, resources, args.User))
        {
            _popup.PopupEntity(Loc.GetString("ce-workbench-cant-craft"), ent, args.User);
            return;
        }

        //Check conditions
        var passConditions = true;
        foreach (var condition in recipe.Conditions)
        {
            if (!condition.CheckCondition(EntityManager, _proto, ent, args.User))
            {
                condition.FailedEffect(EntityManager, _proto, ent, args.User);
                passConditions = false;
            }
            condition.PostCraft(EntityManager, _proto, ent, args.User);
        }

        foreach (var req in recipe.Requirements)
        {
            req.PostCraft(EntityManager, _proto, resources);
        }

        if (passConditions)
        {
            var resultEntities = new HashSet<EntityUid>();
            for (var i = 0; i < recipe.ResultCount; i++)
            {
                var resultEntity = Spawn(recipe.Result);
                resultEntities.Add(resultEntity);
            }

            //We teleport result to workbench AFTER craft.
            foreach (var resultEntity in resultEntities)
            {
                _transform.SetCoordinates(resultEntity, Transform(ent).Coordinates.Offset(new Vector2(_random.NextFloat(-0.25f, 0.25f), _random.NextFloat(-0.25f, 0.25f))));
            }
        }

        UpdateUIRecipes(ent);
        args.Handled = true;
    }

    private void StartCraft(Entity<CEWorkbenchComponent> workbench,
        EntityUid user,
        CEWorkbenchRecipePrototype recipe)
    {
        var craftDoAfter = new CECraftDoAfterEvent
        {
            Recipe = recipe.ID,
        };

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            recipe.CraftTime * workbench.Comp.CraftSpeed,
            craftDoAfter,
            workbench,
            workbench)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _audio.PlayPvs(recipe.OverrideCraftSound ?? workbench.Comp.CraftSound, workbench);
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
