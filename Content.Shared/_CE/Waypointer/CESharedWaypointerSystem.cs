using Content.Shared.Actions;
using Content.Shared.Inventory.Events;

namespace Content.Shared._CE.Waypointer;

/// <summary>
/// Handles granting the Waypointer component and its toggle action to equipees.
/// Waypointers are only drawn for entities already within the player's normal PVS range;
/// there is no server-side PVS override involved.
/// </summary>
public abstract partial class CESharedWaypointerSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CEWaypointerComponent, CEActionToggleWaypointersEvent>(OnActionToggle);
        SubscribeLocalEvent<CEWaypointerComponent, ComponentInit>(OnAddition);
        SubscribeLocalEvent<CEWaypointerComponent, ComponentRemove>(OnRemoval);

        SubscribeLocalEvent<CEWaypointerClothingComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<CEWaypointerClothingComponent, GotUnequippedEvent>(OnUnequip);
    }

    protected virtual void OnActionToggle(Entity<CEWaypointerComponent> mob, ref CEActionToggleWaypointersEvent args)
    {
        if (args.Handled)
            return;

        // Without this in Shared, the action doesn't toggle.
        args.Toggle = true;
        args.Handled = true;
    }

    protected virtual void OnAddition(Entity<CEWaypointerComponent> player, ref ComponentInit args)
    {
        _actions.AddAction(player, ref player.Comp.ActionEntity, player.Comp.ActionProtoId);
    }

    protected virtual void OnRemoval(Entity<CEWaypointerComponent> player, ref ComponentRemove args)
    {
        _actions.RemoveAction(player.Owner, player.Comp.ActionEntity);
    }

    private void OnEquip(Entity<CEWaypointerClothingComponent> clothing, ref GotEquippedEvent args)
    {
        if ((clothing.Comp.SlotFlags & args.SlotFlags) == 0)
            return;

        if (HasComp<CEWaypointerComponent>(args.EquipTarget))
            return;

        var comp = new CEWaypointerComponent
        {
            // We're doing it this way, so ComponentInitEvent doesn't fire without this set.
            WaypointerProtoIds = clothing.Comp.WaypointerProtoIds,
        };

        AddComp(args.EquipTarget, comp);
        Dirty(args.EquipTarget, comp);
    }

    private void OnUnequip(Entity<CEWaypointerClothingComponent> clothing, ref GotUnequippedEvent args)
    {
        if ((clothing.Comp.SlotFlags & args.SlotFlags) == 0)
            return;

        RemComp<CEWaypointerComponent>(args.EquipTarget);
    }
}

[ByRefEvent]
public sealed partial class CEActionToggleWaypointersEvent : InstantActionEvent;
