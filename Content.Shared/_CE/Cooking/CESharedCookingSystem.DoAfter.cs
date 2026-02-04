/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Components;
using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Cooking;

public abstract partial class CESharedCookingSystem
{
    private void InitDoAfter()
    {
        SubscribeLocalEvent<CEFoodCookerComponent, EntParentChangedMessage>(OnParentChanged);

        SubscribeLocalEvent<CEFoodCookerComponent, CECookingDoAfter>(OnCookFinished);
        SubscribeLocalEvent<CEFoodCookerComponent, CEBurningDoAfter>(OnCookBurned);
    }

    private void OnParentChanged(Entity<CEFoodCookerComponent> ent, ref EntParentChangedMessage args)
    {
        StopCooking(ent);
    }

    protected void StartCooking(Entity<CEFoodCookerComponent> ent)
    {
        if (DoAfter.IsRunning(ent.Comp.DoAfterId))
            return;

        _appearance.SetData(ent, CECookingVisuals.Cooking, true);

        // Recipe will be determined at the end of cooking based on current contents
        var doAfterArgs = new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(20), new CECookingDoAfter(), ent)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            RequireCanInteract = false,
        };

        DoAfter.TryStartDoAfter(doAfterArgs, out var doAfterId);
        ent.Comp.DoAfterId = doAfterId;
        _ambientSound.SetAmbience(ent, true);

        Dirty(ent);
    }

    protected void StartBurning(Entity<CEFoodCookerComponent> ent)
    {
        if (DoAfter.IsRunning(ent.Comp.DoAfterId))
            return;

        _appearance.SetData(ent, CECookingVisuals.Burning, true);

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, 20, new CEBurningDoAfter(), ent)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            RequireCanInteract = false,
        };

        DoAfter.TryStartDoAfter(doAfterArgs, out var doAfterId);
        ent.Comp.DoAfterId = doAfterId;
        _ambientSound.SetAmbience(ent, true);

        Dirty(ent);
    }

    protected void StopCooking(Entity<CEFoodCookerComponent> ent)
    {
        if (DoAfter.IsRunning(ent.Comp.DoAfterId))
        {
            DoAfter.Cancel(ent.Comp.DoAfterId);
            ent.Comp.DoAfterId = null;
        }

        _appearance.SetData(ent, CECookingVisuals.Cooking, false);
        _appearance.SetData(ent, CECookingVisuals.Burning, false);

        _ambientSound.SetAmbience(ent, false);

        Dirty(ent);
    }

    protected virtual void OnCookBurned(Entity<CEFoodCookerComponent> ent, ref CEBurningDoAfter args)
    {
        StopCooking(ent);

        if (args.Cancelled || args.Handled)
            return;

        BurntFood(ent);

        args.Handled = true;
    }

    protected virtual void OnCookFinished(Entity<CEFoodCookerComponent> ent, ref CECookingDoAfter args)
    {
        StopCooking(ent);

        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<CEFoodHolderComponent>(ent, out var holder))
            return;

        // Recipe is determined at the end of cooking based on current ingredients
        var recipe = GetRecipe(ent);
        if (recipe is null)
            return;

        Cook(ent, recipe);
        UpdateFoodDataVisuals((ent, holder));

        args.Handled = true;
    }

    private float GetRecipeComplexity(ProtoId<CECookingRecipePrototype>? recipe)
    {
        if (recipe is null)
            return 0;

        if (!_proto.Resolve(recipe.Value, out var indexedRecipe))
            return 0;

        return indexedRecipe.GetComplexity();
    }
}

[Serializable, NetSerializable]
public sealed partial class CECookingDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CEBurningDoAfter : SimpleDoAfterEvent;
