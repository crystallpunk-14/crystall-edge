using Content.Shared._CE.Workbench;

namespace Content.Server._CE.Workbench;

public sealed partial class CEWorkbenchSystem
{
    private void InitAutoCrafter()
    {

        SubscribeLocalEvent<CEWorkbenchAutoCrafterComponent, CEWorkbenchUiSetAutoCraftMessage>(OnSetRecipe);
    }

    private void OnSetRecipe(Entity<CEWorkbenchAutoCrafterComponent> ent, ref CEWorkbenchUiSetAutoCraftMessage args)
    {
        if (!_workbenchQuery.TryComp(ent, out var workbench))
            return;

        if (!workbench.Recipes.Contains(args.Recipe))
            return;

        ent.Comp.SelectedRecipe = args.Recipe;
    }
}
