using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Workbench;
using Content.Shared.DoAfter;
using Content.Shared.Power;

namespace Content.Server._CE.Workbench;

public sealed partial class CEWorkbenchSystem
{
    private void InitAutoCrafter()
    {
        SubscribeLocalEvent<CEWorkbenchAutoCrafterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CEWorkbenchAutoCrafterComponent, CECraftDoAfterEvent>(OnFinishAutoCraft);
        SubscribeLocalEvent<CEWorkbenchAutoCrafterComponent, CEWorkbenchUiClickRecipeMessage>(OnClickMessage);
    }

    private void OnFinishAutoCraft(Entity<CEWorkbenchAutoCrafterComponent> ent, ref CECraftDoAfterEvent args)
    {
        ent.Comp.ActiveDoAfter = null;

        if (args.Cancelled || args.Handled)
            return;

        if (!_proto.Resolve(args.Recipe, out var recipe))
            return;

        if (!_workbenchQuery.TryComp(ent, out var workbench))
            return;

        var getResource = new CEWorkbenchGetResourcesEvent();
        RaiseLocalEvent(ent.Owner, getResource);

        var resources = getResource.Resources;

        if (!CanCraftRecipe(recipe, resources))
        {
            _popup.PopupEntity(Loc.GetString("ce-workbench-cant-craft"), ent);
            return;
        }

        ConsumeRecipeResources(recipe, resources);

        if (CheckRecipeConditions(recipe, ent, null))
            SpawnRecipeResult(recipe, ent);

        SpawnAtPosition(workbench.Vfx, Transform(ent).Coordinates);
        UpdateUIRecipes(ent.Owner);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEWorkbenchAutoCrafterComponent, CEWorkbenchComponent>();
        while (query.MoveNext(out var uid, out var autoCrafter, out var workbench))
        {
            if (_timing.CurTime < autoCrafter.NextCraftTime)
                continue;
            autoCrafter.NextCraftTime = _timing.CurTime + autoCrafter.CraftDelay; // Just for prevent spamming checks

            if (workbench.SelectedRecipe is null)
                continue;

            if (autoCrafter.ActiveDoAfter is not null)
                continue;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            if (!_proto.Resolve(workbench.SelectedRecipe.Value, out var recipe))
                continue;

            // Check if we have resources to craft before starting DoAfter
            var getResource = new CEWorkbenchGetResourcesEvent();
            RaiseLocalEvent(uid, getResource);

            if (!CanCraftRecipe(recipe, getResource.Resources))
                continue;

            var craftDoAfter = new CECraftDoAfterEvent
            {
                Recipe = workbench.SelectedRecipe.Value,
            };

            var craftTime = recipe.CraftTime * workbench.CraftSpeed;
            var doAfterArgs = new DoAfterArgs(EntityManager,
                uid,
                craftTime,
                craftDoAfter,
                uid,
                uid)
            {
                BreakOnMove = true,
                BreakOnDamage = false,
                NeedHand = false,
            };

            _doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId);
            _audio.PlayPvs(recipe.OverrideCraftSound ?? workbench.CraftSound, uid);
            autoCrafter.ActiveDoAfter = doAfterId;
            autoCrafter.NextCraftTime = _timing.CurTime + autoCrafter.CraftDelay + craftTime;
        }
    }

    private void OnPowerChanged(Entity<CEWorkbenchAutoCrafterComponent> ent, ref PowerChangedEvent args)
    {
        BreakCrafting(ent);
    }

    private void OnClickMessage(Entity<CEWorkbenchAutoCrafterComponent> ent, ref CEWorkbenchUiClickRecipeMessage args)
    {
        BreakCrafting(ent);
    }

    private void BreakCrafting(Entity<CEWorkbenchAutoCrafterComponent> ent)
    {
        if (ent.Comp.ActiveDoAfter is null)
            return;

        _doAfter.Cancel(ent.Comp.ActiveDoAfter);
        ent.Comp.ActiveDoAfter = null;
    }
}
