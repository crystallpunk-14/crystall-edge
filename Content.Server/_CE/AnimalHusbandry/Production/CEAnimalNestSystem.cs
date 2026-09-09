using Content.Server._CE.AnimalHusbandry.Reproduction;
using Content.Server._CE.GOAP;
using Content.Server.NPC.Systems;
using Content.Shared._CE.AnimalHusbandry.Reproduction;
using Content.Shared._CE.Containers;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.EntityConditions;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.AnimalHusbandry.Production;

public sealed partial class CEGOAPRoostAction : CEGOAPActionBase<CEGOAPRoostAction>;

/// <summary>Maintains nest targeting and the bird's bounded residence after laying.</summary>
public sealed partial class CEAnimalNestSystem : CEGOAPActionSystem<CEGOAPRoostAction>
{
    private const string RoostState = "CEAnimalRoosting";
    [Dependency] private NPCSteeringSystem _steering = default!;
    [Dependency] private CEAnimalIncubationSystem _incubation = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    [SubscribeLocalEvent]
    private void OnHostStartup(Entity<CEAnimalNestComponent> ent, ref ComponentStartup args) => SynchronizeNest(ent);

    [SubscribeLocalEvent]
    private void OnSlotsChanged(Entity<CEAnimalNestComponent> ent, ref CEItemSlotsChangedEvent args) => SynchronizeNest(ent);

    private void SynchronizeNest(Entity<CEAnimalNestComponent> ent)
    {
        RefreshAvailability(ent);
        if (TryComp<ItemSlotsComponent>(ent, out var slots))
        {
            foreach (var slot in slots.Slots.Values)
            {
                if (slot.Item is { } egg)
                    _incubation.Synchronize(egg);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnHostShutdown(Entity<CEAnimalNestComponent> ent, ref ComponentShutdown args)
    {
        foreach (var uid in ent.Comp.Residents)
        {
            if (!TryComp<CEAnimalRoostComponent>(uid, out var rest) || rest.Host != ent.Owner)
                continue;
            rest.Host = null;
            SetState((uid, rest));
        }
        ent.Comp.Residents.Clear();
        if (!TerminatingOrDeleted(ent))
            RemComp<CEAnimalNestAvailableComponent>(ent);
        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;
        foreach (var slot in slots.Slots.Values)
        {
            if (slot.Item is { } egg)
                _incubation.Pause(egg);
        }
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<CEAnimalNestComponent> ent, ref EntInsertedIntoContainerMessage args) => RefreshAvailability(ent);

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CEAnimalNestComponent> ent, ref EntRemovedFromContainerMessage args) => RefreshAvailability(ent);

    public void RefreshAvailability(Entity<CEAnimalNestComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;
        if (ent.Comp.LifeStage >= ComponentLifeStage.Stopping)
        {
            RemComp<CEAnimalNestAvailableComponent>(ent);
            return;
        }
        var host = ent.Owner;
        ent.Comp.Residents.RemoveWhere(uid => !Exists(uid) ||
            !TryComp<CEAnimalRoostComponent>(uid, out var rest) || rest.Host != host);
        var available = false;
        if (ent.Comp.Residents.Count < ent.Comp.ResidentCapacity && TryComp<ItemSlotsComponent>(ent, out var slots) &&
            slots.LifeStage < ComponentLifeStage.Stopping)
        {
            foreach (var slot in slots.Slots.Values)
            {
                if (!slot.Locked && slot.ContainerSlot != null && !slot.HasItem)
                {
                    available = true;
                    break;
                }
            }
        }
        if (available)
            EnsureComp<CEAnimalNestAvailableComponent>(ent);
        else
            RemComp<CEAnimalNestAvailableComponent>(ent);
    }

    public void RestAfterLaying(EntityUid producer, Entity<CEAnimalNestComponent> host)
    {
        if (!TryComp<CEAnimalRoostComponent>(producer, out var rest))
            return;
        var ent = new Entity<CEAnimalRoostComponent>(producer, rest);
        Release(ent);
        if (rest.MinimumDuration < TimeSpan.Zero || rest.MaximumDuration < rest.MinimumDuration ||
            host.Comp.Residents.Count >= host.Comp.ResidentCapacity)
            return;

        host.Comp.Residents.Add(producer);
        rest.Host = host;
        rest.Until = _timing.CurTime + _random.Next(rest.MinimumDuration, rest.MaximumDuration);
        SetState(ent);
        RefreshAvailability(host);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<CEAnimalRoostComponent>();
        while (query.MoveNext(out var uid, out var rest))
        {
            if (rest.Host is { } host && (!HasComp<CEAnimalNestComponent>(host) ||
                _timing.CurTime >= rest.Until || !HasComp<CEActiveGOAPComponent>(uid)))
                Release((uid, rest));
        }
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CEAnimalRoostComponent> ent, ref ComponentStartup args) => SetState(ent);

    [SubscribeLocalEvent]
    private void OnRefresh(Entity<CEAnimalRoostComponent> ent, ref CEGOAPSensorRefreshEvent args) => SetState(ent);

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CEAnimalRoostComponent> ent, ref ComponentShutdown args) => Release(ent);

    private void SetState(Entity<CEAnimalRoostComponent> ent)
    {
        if (!TryComp<CEGOAPComponent>(ent, out var goap))
            return;
        var value = ent.Comp.Host != null;
        if (goap.WorldState.TryGetValue(RoostState, out var previous) && previous == value)
            return;
        goap.WorldState[RoostState] = value;
        goap.NextPlanTime = TimeSpan.Zero;
    }

    private void Release(Entity<CEAnimalRoostComponent> ent)
    {
        var host = ent.Comp.Host;
        ent.Comp.Host = null;
        if (TryComp<CEAnimalNestComponent>(host, out var nest))
        {
            nest.Residents.Remove(ent.Owner);
            RefreshAvailability((host.Value, nest));
        }
        SetState(ent);
    }

    protected override void OnActionStartup(Entity<CEGOAPComponent> ent, ref CEGOAPActionStartupEvent<CEGOAPRoostAction> args)
    {
        _steering.Unregister(ent);
    }

    protected override void OnActionUpdate(Entity<CEGOAPComponent> ent, ref CEGOAPActionUpdateEvent<CEGOAPRoostAction> args)
    {
        if (!TryComp<CEAnimalRoostComponent>(ent, out var rest) || rest.Host is not { } host ||
            !Exists(host) || _timing.CurTime >= rest.Until ||
            !_conditions.TryConditions(ent.Owner, rest.Conditions) ||
            !Transform(ent).Coordinates.TryDistance(EntityManager, Transform(host).Coordinates, out var distance) || distance > rest.HostRange)
            args.Status = CEGOAPActionStatus.Finished;
    }

    protected override void OnActionShutdown(Entity<CEGOAPComponent> ent, ref CEGOAPActionShutdownEvent<CEGOAPRoostAction> args)
    {
        if (TryComp<CEAnimalRoostComponent>(ent, out var rest))
            Release((ent, rest));
    }
}
