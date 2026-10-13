/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Events;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Containers;

namespace Content.Shared._CE.Cooking;

public abstract partial class CESharedCookingSystem
{
    [Dependency] private StatusEffectsSystem _statusEffect = default!;

    [SubscribeLocalEvent]
    private void OnEat(Entity<CEFoodHolderComponent> ent, ref IngestedEvent args)
    {
        if (ent.Comp.FoodData is null)
            return;

        var eatAmount = args.Split.Volume;
        var recipeComplexity = GetRecipeComplexity(ent.Comp.FoodData.CurrentRecipe);
        foreach (var (effect, duration) in ent.Comp.FoodData.StatusEffects)
        {
            var effectDuration = eatAmount * duration * Math.Max(recipeComplexity, 1);
            _statusEffect.TryAddStatusEffectDuration(args.Target, effect, TimeSpan.FromSeconds((float)effectDuration));
        }
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<CEFoodHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<CEFoodHolderComponent>(args.Used, out var usedComp))
            return;

        Entity<CEFoodHolderComponent> used = (args.Used, usedComp);

        // Food can be moved in both directions: cooker used on a plate, or plate used on a cooker
        if (used.Comp.CanGiveFood && ent.Comp.CanAcceptFood)
            TryTransferFood(ent, used);
        else if (ent.Comp.CanGiveFood && used.Comp.CanAcceptFood)
            TryTransferFood(used, ent);
        else
            return;

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnHolderSolutionChanged(Entity<CEFoodHolderComponent> ent, ref SolutionChangedEvent args)
    {
        if (ent.Comp.FoodData is null)
            return;

        if (args.Solution.Comp.Id != ent.Comp.SolutionId)
            return;

        if (args.Solution.Comp.Solution.Volume > 0)
            return;

        ClearFoodData(ent);
    }

    /// <summary>
    /// Finished food cannot be diluted or mixed with anything else.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSolutionTransferAttempt(Entity<CEFoodHolderComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        if (args.To != ent.Owner || ent.Comp.FoodData is null)
            return;

        args.Cancel(Loc.GetString("ce-cooking-popup-no-refill", ("name", MetaData(ent).EntityName)));
    }

    [SubscribeLocalEvent]
    private void OnBeforeInject(Entity<CEFoodHolderComponent> ent, ref TargetBeforeInjectEvent args)
    {
        if (ent.Comp.FoodData is null)
            return;

        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<CEFoodCookerComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<CEFoodHolderComponent>(ent, out var holder))
            return;

        if (holder.FoodData is null)
            return;

        //Canceling inserting entities if FoodData not empty

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("ce-cooking-popup-not-empty", ("name", MetaData(ent).EntityName)), ent);

        args.Cancel();
    }
}
