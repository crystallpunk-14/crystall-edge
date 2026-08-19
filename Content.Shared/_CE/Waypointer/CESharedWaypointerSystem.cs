using Content.Shared.Actions;
using Content.Shared.Inventory.Events;

namespace Content.Shared._CE.Waypointer;

/// <summary>
/// This solely handles giving the Waypoint component to equipees. This cannot be done on client, or else it would.
/// </summary>
public abstract class CESharedWaypointerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<CEWaypointerComponent, CEActionToggleWaypointersEvent>(OnActionToggle);

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
