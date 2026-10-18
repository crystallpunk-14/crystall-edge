/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Cooking;

public abstract partial class CESharedCookingSystem
{
    private static readonly TimeSpan CookingTime = TimeSpan.FromSeconds(20);

    [SubscribeLocalEvent]
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
        StartDoAfter(ent, new CECookingDoAfter());
    }

    protected void StartBurning(Entity<CEFoodCookerComponent> ent)
    {
        if (DoAfter.IsRunning(ent.Comp.DoAfterId))
            return;

        _appearance.SetData(ent, CECookingVisuals.Burning, true);

        StartDoAfter(ent, new CEBurningDoAfter());
    }

    private void StartDoAfter(Entity<CEFoodCookerComponent> ent, DoAfterEvent ev)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, ent, CookingTime, ev, ent)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            RequireCanInteract = false,
        };

        DoAfter.TryStartDoAfter(doAfterArgs, out var doAfterId);
        ent.Comp.DoAfterId = doAfterId;
        _ambientSound.SetAmbience(ent, true);
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
    }

    [SubscribeLocalEvent]
    private void OnCookBurned(Entity<CEFoodCookerComponent> ent, ref CEBurningDoAfter args)
    {
        StopCooking(ent);

        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<CEFoodHolderComponent>(ent, out var holder))
            return;

        BurntFood(ent, holder);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    protected virtual void OnCookFinished(Entity<CEFoodCookerComponent> ent, ref CECookingDoAfter args)
    {
        StopCooking(ent);

        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<CEFoodHolderComponent>(ent, out var holder))
            return;

        // Recipe is determined at the end of cooking based on current ingredients
        var recipe = GetRecipe(ent, holder);
        if (recipe is null)
            return;

        Cook(ent, holder, recipe);

        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class CECookingDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CEBurningDoAfter : SimpleDoAfterEvent;
