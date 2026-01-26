/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Containers;

namespace Content.Shared._CE.Cooking;

public abstract partial class CESharedCookingSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    private void InitTransfer()
    {
        SubscribeLocalEvent<CEFoodHolderComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CEFoodHolderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CEFoodHolderComponent, SolutionContainerChangedEvent>(OnHolderSolutionChanged);
        SubscribeLocalEvent<CEFoodHolderComponent, IngestedEvent>(OnEat);

        SubscribeLocalEvent<CEFoodCookerComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
    }

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

    private void OnInteractUsing(Entity<CEFoodHolderComponent> target, ref InteractUsingEvent args)
    {
        if (!TryComp<CEFoodHolderComponent>(args.Used, out var used))
            return;

        TryTransferFood(target, (args.Used, used));
    }

    private void OnAfterInteract(Entity<CEFoodHolderComponent> ent, ref AfterInteractEvent args)
    {
        if (!TryComp<CEFoodHolderComponent>(args.Target, out var target))
            return;

        TryTransferFood(ent, (args.Target.Value, target));
    }

    private void OnHolderSolutionChanged(Entity<CEFoodHolderComponent> ent, ref SolutionContainerChangedEvent args)
    {
        // Check if this is the solution we care about
        if (ent.Comp.SolutionId == null || ent.Comp.SolutionId != args.SolutionId)
            return;

        // Clear food data when solution is empty
        if (args.Solution.Volume == 0)
        {
            SetFoodData(ent, null);
            UpdateFoodDataVisuals(ent);
        }
    }

    private void OnInsertAttempt(Entity<CEFoodCookerComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

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
