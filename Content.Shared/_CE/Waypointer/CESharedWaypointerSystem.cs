using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Waypointer;

/// <summary>
/// Aggregates every active waypointer source (clothing, marker components granted by other systems, etc.)
/// on an entity into its CEWaypointerComponent. Sources contribute by handling RefreshWaypointersEvent and
/// must call RefreshWaypointers whenever their own contribution changes.
/// Waypointers are only drawn for entities already within the player's normal PVS range;
/// there is no server-side PVS override involved.
/// </summary>
public abstract partial class CESharedWaypointerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<CEWaypointerComponent, ComponentInit>(OnAddition);
        SubscribeLocalEvent<CEWaypointerComponent, ComponentRemove>(OnRemoval);

        SubscribeLocalEvent<CEWaypointerClothingComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<CEWaypointerClothingComponent, GotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<CEWaypointerClothingComponent, InventoryRelayedEvent<CERefreshWaypointersEvent>>(OnClothingRefresh);
    }

    protected virtual void OnAddition(Entity<CEWaypointerComponent> player, ref ComponentInit args)
    {
    }

    protected virtual void OnRemoval(Entity<CEWaypointerComponent> player, ref ComponentRemove args)
    {
    }

    /// <summary>
    /// Recomputes the set of active waypointer prototypes for this entity from every contributing source
    /// and updates CEWaypointerComponent accordingly, adding/removing it as the resolved set becomes non-empty/empty.
    /// </summary>
    public void RefreshWaypointers(EntityUid mob)
    {
        var ev = new CERefreshWaypointersEvent();
        RaiseLocalEvent(mob, ref ev);

        if (ev.WaypointerProtoIds.Count == 0)
        {
            RemCompDeferred<CEWaypointerComponent>(mob);
            return;
        }

        var comp = EnsureComp<CEWaypointerComponent>(mob);
        if (comp.WaypointerProtoIds.SetEquals(ev.WaypointerProtoIds))
            return;

        comp.WaypointerProtoIds = ev.WaypointerProtoIds;
        Dirty(mob, comp);
    }

    private void OnEquip(Entity<CEWaypointerClothingComponent> clothing, ref GotEquippedEvent args)
    {
        if ((clothing.Comp.SlotFlags & args.SlotFlags) == 0)
            return;

        RefreshWaypointers(args.EquipTarget);
    }

    private void OnUnequip(Entity<CEWaypointerClothingComponent> clothing, ref GotUnequippedEvent args)
    {
        if ((clothing.Comp.SlotFlags & args.SlotFlags) == 0)
            return;

        RefreshWaypointers(args.EquipTarget);
    }

    private void OnClothingRefresh(Entity<CEWaypointerClothingComponent> clothing, ref InventoryRelayedEvent<CERefreshWaypointersEvent> args)
    {
        args.Args.WaypointerProtoIds.UnionWith(clothing.Comp.WaypointerProtoIds);
    }
}

[ByRefEvent]
public record struct CERefreshWaypointersEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
    public HashSet<ProtoId<CEWaypointerPrototype>> WaypointerProtoIds = new();
}
