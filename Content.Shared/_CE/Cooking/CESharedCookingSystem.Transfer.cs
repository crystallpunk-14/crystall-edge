/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Shared._CE.Cooking;

public abstract partial class CESharedCookingSystem
{
    private void InitTransfer()
    {
        SubscribeLocalEvent<CEFoodHolderComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CEFoodHolderComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<CEFoodCookerComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
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

    private void OnInsertAttempt(Entity<CEFoodCookerComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<CEFoodHolderComponent>(ent, out var holder))
            return;

        if (holder.FoodData is not null)
        {
            _popup.PopupEntity(Loc.GetString("ce-cooking-popup-not-empty", ("name", MetaData(ent).EntityName)), ent);
            args.Cancel();
        }
    }
}
