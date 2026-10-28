using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Analyzers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Waypointer;

/// <summary>
/// Aggregates every active waypointer source (clothing, marker components granted by other systems, etc.)
/// on an entity into its CEWaypointerComponent. Sources contribute by handling RefreshWaypointersEvent and
/// must call RefreshWaypointers whenever their own contribution changes.
/// Component-tracked waypointers are only drawn for entities already within the player's normal PVS range.
/// Specific tracked entities can be PVS-overridden by the server, see <see cref="CEWaypointerPrototype.PvsOverride"/>.
/// </summary>
public abstract partial class CESharedWaypointerSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

    [SubscribeLocalEvent]
    protected virtual void OnAddition(Entity<CEWaypointerComponent> player, ref ComponentInit args)
    {
    }

    [SubscribeLocalEvent]
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

        // Tracked entity sources resolve their targets from server-only data, so the client keeps whatever the server last sent.
        if (_net.IsClient && TryComp<CEWaypointerComponent>(mob, out var existing))
            ev.TrackedEntities = new Dictionary<EntityUid, ProtoId<CEWaypointerPrototype>>(existing.TrackedEntities);

        if (ev.WaypointerProtoIds.Count == 0 && ev.TrackedEntities.Count == 0)
        {
            RemCompDeferred<CEWaypointerComponent>(mob);
            return;
        }

        var comp = EnsureComp<CEWaypointerComponent>(mob);
        if (comp.WaypointerProtoIds.SetEquals(ev.WaypointerProtoIds) && TrackedEntitiesEqual(comp.TrackedEntities, ev.TrackedEntities))
            return;

        comp.WaypointerProtoIds = ev.WaypointerProtoIds;
        comp.TrackedEntities = ev.TrackedEntities;
        Dirty(mob, comp);

        OnTrackedEntitiesChanged((mob, comp));
    }

    /// <summary>
    /// Called after a refresh changed the owner's waypointers.
    /// </summary>
    protected virtual void OnTrackedEntitiesChanged(Entity<CEWaypointerComponent> player)
    {
    }

    private static bool TrackedEntitiesEqual(
        Dictionary<EntityUid, ProtoId<CEWaypointerPrototype>> a,
        Dictionary<EntityUid, ProtoId<CEWaypointerPrototype>> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var (uid, proto) in a)
        {
            if (!b.TryGetValue(uid, out var other) || other != proto)
                return false;
        }

        return true;
    }

    [SubscribeLocalEvent]
    private void OnEquip(Entity<CEWaypointerClothingComponent> clothing, ref GotEquippedEvent args)
    {
        if ((clothing.Comp.SlotFlags & args.SlotFlags) == 0)
            return;

        RefreshWaypointers(args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnUnequip(Entity<CEWaypointerClothingComponent> clothing, ref GotUnequippedEvent args)
    {
        if ((clothing.Comp.SlotFlags & args.SlotFlags) == 0)
            return;

        RefreshWaypointers(args.EquipTarget);
    }

    [SubscribeLocalEvent]
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

    /// <summary>
    /// Specific entities to point at, each with the waypointer prototype it is drawn with.
    /// </summary>
    public Dictionary<EntityUid, ProtoId<CEWaypointerPrototype>> TrackedEntities = new();
}
