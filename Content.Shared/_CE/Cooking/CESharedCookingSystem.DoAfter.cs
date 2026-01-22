/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Components;
using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Temperature;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Cooking;

public abstract partial class CESharedCookingSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private void InitDoAfter()
    {
        SubscribeLocalEvent<CEFoodCookerComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
        SubscribeLocalEvent<CEFoodCookerComponent, EntParentChangedMessage>(OnParentChanged);

        SubscribeLocalEvent<CEFoodCookerComponent, CECookingDoAfter>(OnCookFinished);
        SubscribeLocalEvent<CEFoodCookerComponent, CEBurningDoAfter>(OnCookBurned);
    }

    private void UpdateDoAfter(float frameTime)
    {
        var query = EntityQueryEnumerator<CEFoodCookerComponent>();
        while (query.MoveNext(out var uid, out var cooker))
        {
            if (_timing.CurTime > cooker.LastHeatingTime + cooker.HeatingFrequencyRequired)
                StopCooking((uid, cooker));
        }
    }

    private void OnTemperatureChange(Entity<CEFoodCookerComponent> ent, ref OnTemperatureChangeEvent args)
    {
        if (args.TemperatureDelta <= 0)
            return;

        if (!Container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        if (!TryComp<CEFoodHolderComponent>(ent, out var holder))
            return;

        if (container.ContainedEntities.Count <= 0 &&
            holder.FoodData is null) // We can be either cooking (null foodData) or burning (zero contained)
        {
            StopCooking(ent);
            return;
        }

        ent.Comp.LastHeatingTime = _timing.CurTime;

        if (!_doAfter.IsRunning(ent.Comp.DoAfterId) && holder.FoodData is null)
        {
            var recipe = GetRecipe(ent);
            if (recipe is not null)
                StartCooking(ent, recipe);
        }
        else
        {
            StartBurning(ent);
        }
    }

    private void OnParentChanged(Entity<CEFoodCookerComponent> ent, ref EntParentChangedMessage args)
    {
        StopCooking(ent);
    }

    private void StartCooking(Entity<CEFoodCookerComponent> ent, CECookingRecipePrototype recipe)
    {
        if (_doAfter.IsRunning(ent.Comp.DoAfterId))
            return;

        _appearance.SetData(ent, CECookingVisuals.Cooking, true);

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, recipe.CookingTime, new CECookingDoAfter(recipe.ID), ent)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            RequireCanInteract = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId);
        ent.Comp.DoAfterId = doAfterId;
        _ambientSound.SetAmbience(ent, true);
    }

    private void StartBurning(Entity<CEFoodCookerComponent> ent)
    {
        if (_doAfter.IsRunning(ent.Comp.DoAfterId))
            return;

        _appearance.SetData(ent, CECookingVisuals.Burning, true);

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, 20, new CEBurningDoAfter(), ent)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            RequireCanInteract = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId);
        ent.Comp.DoAfterId = doAfterId;
        _ambientSound.SetAmbience(ent, true);
    }

    private void StopCooking(Entity<CEFoodCookerComponent> ent)
    {
        if (_doAfter.IsRunning(ent.Comp.DoAfterId))
        {
            _doAfter.Cancel(ent.Comp.DoAfterId);
            ent.Comp.DoAfterId = null;
        }

        _appearance.SetData(ent, CECookingVisuals.Cooking, false);
        _appearance.SetData(ent, CECookingVisuals.Burning, false);

        _ambientSound.SetAmbience(ent, false);
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

        if (!_proto.Resolve(args.Recipe, out var indexedRecipe))
            return;

        Cook(ent, indexedRecipe);
        UpdateFoodDataVisuals((ent, holder));

        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class CECookingDoAfter : DoAfterEvent
{
    [DataField]
    public ProtoId<CECookingRecipePrototype> Recipe;

    public CECookingDoAfter(ProtoId<CECookingRecipePrototype> recipe)
    {
        Recipe = recipe;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class CEBurningDoAfter : SimpleDoAfterEvent;
