using System.Numerics;
using Content.Shared._CE.Workbench;
using Content.Shared._CE.Workbench.Prototypes;
using Content.Shared.DoAfter;

namespace Content.Server._CE.Workbench;

public sealed partial class CEWorkbenchSystem
{
    private void InitUserCrafter()
    {
        SubscribeLocalEvent<CEWorkbenchUserCrafterComponent, CEWorkbenchUiClickRecipeMessage>(OnCraft);
        SubscribeLocalEvent<CEWorkbenchUserCrafterComponent, CECraftDoAfterEvent>(OnUserCraftFinished);
    }

    private void OnCraft(Entity<CEWorkbenchUserCrafterComponent> ent, ref CEWorkbenchUiClickRecipeMessage args)
    {
        if (!_workbenchQuery.TryComp(ent, out var workbench))
            return;

        if (!workbench.Recipes.Contains(args.Recipe))
            return;

        if (!_proto.Resolve(args.Recipe, out var prototype))
            return;

        StartUserCraft(ent, args.Actor, prototype, workbench);
    }

    private void StartUserCraft(Entity<CEWorkbenchUserCrafterComponent> ent,
        EntityUid user,
        CEWorkbenchRecipePrototype recipe,
        CEWorkbenchComponent workbenchComp)
    {
        var craftDoAfter = new CECraftDoAfterEvent
        {
            Recipe = recipe.ID,
        };

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            recipe.CraftTime * workbenchComp.CraftSpeed,
            craftDoAfter,
            ent,
            ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _audio.PlayPvs(recipe.OverrideCraftSound ?? workbenchComp.CraftSound, ent);
    }

    private void OnUserCraftFinished(Entity<CEWorkbenchUserCrafterComponent> ent, ref CECraftDoAfterEvent args)
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

        ConsumeRecipeResources(recipe, resources);

        if (CheckRecipeConditions(recipe, ent, args.User))
            SpawnRecipeResult(recipe, ent);

        UpdateUIRecipes(ent.Owner);
        args.Handled = true;
    }
}
