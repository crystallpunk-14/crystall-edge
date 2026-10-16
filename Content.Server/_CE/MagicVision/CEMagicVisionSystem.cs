using Content.Shared._CE.MagicVision.Components;
using Content.Shared._CE.MagicVision.Events;
using Content.Shared.Actions;
using Content.Shared.Eye;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Toggleable;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server._CE.MagicVision;

/// <summary>
/// Grants entities magic vision (the <see cref="CEMagicVisionComponent"/> marker, which drives the
/// visibility mask and the client-side overlay) based on however many sources currently want them to
/// have it - clothing (<see cref="CEMagicVisionClothingComponent"/>), the toggleable magic vision skill
/// (<see cref="CEMagicVisionToggleComponent"/>), and potentially other sources in the future,
/// all decided fresh each time via <see cref="CECheckMagicVisionEvent"/>. This
/// is entirely server-authoritative: the component is networked and drives PVS filtering, so the
/// client only ever mirrors it from replicated state and never predicts it.
/// </summary>
public sealed partial class CEMagicVisionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedEyeSystem _eye = default!;

    [SubscribeLocalEvent]
    private void OnGetVisMask(Entity<CEMagicVisionComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.CEMagicVision;
    }

    [SubscribeLocalEvent]
    private void OnClothingCheckVision(Entity<CEMagicVisionClothingComponent> ent, ref InventoryRelayedEvent<CECheckMagicVisionEvent> args)
    {
        args.Args.GrantVision();
    }

    [SubscribeLocalEvent]
    private void OnClothingEquipped(Entity<CEMagicVisionClothingComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & CECheckMagicVisionEvent.Slots) == 0)
            return;

        RefreshMagicVision(args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnClothingUnequipped(Entity<CEMagicVisionClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if ((args.SlotFlags & CECheckMagicVisionEvent.Slots) == 0)
            return;

        RefreshMagicVision(args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnToggleMapInit(Entity<CEMagicVisionToggleComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnToggleShutdown(Entity<CEMagicVisionToggleComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);

        // The component is still attached during shutdown, so drop its contribution before refreshing.
        ent.Comp.Enabled = false;

        if (!TerminatingOrDeleted(ent))
            RefreshMagicVision(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnToggleAction(Entity<CEMagicVisionToggleComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled || args.Action.Owner != ent.Comp.ActionEntity)
            return;

        args.Handled = true;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        _actions.SetToggled(ent.Comp.ActionEntity, ent.Comp.Enabled);

        RefreshMagicVision(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnToggleCheckVision(Entity<CEMagicVisionToggleComponent> ent, ref CECheckMagicVisionEvent args)
    {
        if (ent.Comp.Enabled)
            args.GrantVision();
    }

    /// <summary>
    /// Recomputes whether <see cref="uid"/> currently has magic vision by asking every subscribed
    /// source via <see cref="CECheckMagicVisionEvent"/>, then adds or removes
    /// <see cref="CEMagicVisionComponent"/> to match. Sources (clothing, skills, etc.) should call
    /// this whenever their own contribution to magic vision might have changed - e.g. on equip/unequip
    /// or on skill learned/forgotten.
    /// </summary>
    /// <remarks>
    /// The visibility mask is refreshed here, right after the component add/remove completes, rather
    /// than from that component's own ComponentStartup/ComponentShutdown - during ComponentShutdown the
    /// component is still technically attached, so a GetVisMaskEvent raised from there would see it as
    /// present and re-add the flag it's supposed to be losing.
    /// </remarks>
    public void RefreshMagicVision(EntityUid uid)
    {
        var ev = new CECheckMagicVisionEvent();
        RaiseLocalEvent(uid, ev);

        if (!ev.HasVision)
        {
            if (RemComp<CEMagicVisionComponent>(uid))
                _eye.RefreshVisibilityMask(uid);

            return;
        }

        var isNew = !HasComp<CEMagicVisionComponent>(uid);
        var comp = EnsureComp<CEMagicVisionComponent>(uid);

        if (isNew)
            _eye.RefreshVisibilityMask(uid);

        if (comp.ShowOverlay != ev.ShowOverlay)
        {
            comp.ShowOverlay = ev.ShowOverlay;
            Dirty(uid, comp);
        }
    }
}
