using Content.Server._CE.GOAP;
using Content.Server.NPC.Systems;
using Content.Shared._CE.EntitySlots;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared.EntityConditions;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Server._CE.Production;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEProductionRestComponent : Component
{
    [DataField(required: true)] public TimeSpan MinimumDuration;
    [DataField(required: true)] public TimeSpan MaximumDuration;
    [DataField(required: true)] public string StateKey = default!;
    [DataField] public float HostRange = 0.9f;
    [DataField] public EntityCondition[] Conditions = [];
    [DataField] public EntityUid? Host;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Until;
}

/// <summary>Resident reservations are separate from the host's physical product slots.</summary>
[RegisterComponent]
public sealed partial class CEProductionRestHostComponent : Component
{
    [DataField(required: true)] public int Capacity;
    [DataField] public HashSet<EntityUid> Residents = new();
}

[RegisterComponent]
public sealed partial class CEProductionRestHostAvailableComponent : Component
{
}

public sealed partial class CEGOAPRestAtProductionHostAction : CEGOAPActionBase<CEGOAPRestAtProductionHostAction>
{
}

/// <summary>Reserves a production host for a bounded rest after successful production.</summary>
public sealed partial class CEProductionRestSystem : CEGOAPActionSystem<CEGOAPRestAtProductionHostAction>
{
    [Dependency] private NPCSteeringSystem _steering = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEProductionRestComponent, CEFixedSlotEntityProducedEvent>(OnProduced);
        SubscribeLocalEvent<CEProductionRestComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CEProductionRestHostComponent, MapInitEvent>(OnHostInit);
        SubscribeLocalEvent<CEProductionRestHostComponent, ComponentShutdown>(OnHostShutdown);
    }

    private void OnHostInit(Entity<CEProductionRestHostComponent> ent, ref MapInitEvent args) => UpdateAvailability(ent);

    private void OnHostShutdown(Entity<CEProductionRestHostComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Owner))
            RemComp<CEProductionRestHostAvailableComponent>(ent);
    }

    private void UpdateAvailability(Entity<CEProductionRestHostComponent> ent)
    {
        if (ent.Comp.Residents.Count < ent.Comp.Capacity)
            EnsureComp<CEProductionRestHostAvailableComponent>(ent);
        else
            RemComp<CEProductionRestHostAvailableComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<CEProductionRestComponent>();
        while (query.MoveNext(out var uid, out var rest))
        {
            if (rest.Host is { } host && (!HasComp<CEProductionRestHostComponent>(host) || _timing.CurTime >= rest.Until ||
                !HasComp<CEActiveGOAPComponent>(uid)))
                Release((uid, rest));
        }
    }

    private void OnProduced(Entity<CEProductionRestComponent> ent, ref CEFixedSlotEntityProducedEvent args)
    {
        Release(ent);
        if (ent.Comp.MinimumDuration < TimeSpan.Zero || ent.Comp.MaximumDuration < ent.Comp.MinimumDuration ||
            !TryComp<CEProductionRestHostComponent>(args.Target, out var host))
            return;

        var target = args.Target;
        host.Residents.RemoveWhere(uid => !Exists(uid) ||
            !TryComp<CEProductionRestComponent>(uid, out var rest) || rest.Host != target);
        if (host.Residents.Count >= host.Capacity)
            return;

        host.Residents.Add(ent.Owner);
        UpdateAvailability((args.Target, host));
        ent.Comp.Host = args.Target;
        ent.Comp.Until = _timing.CurTime + _random.Next(ent.Comp.MinimumDuration, ent.Comp.MaximumDuration);
        SetState(ent, true);
    }

    private void SetState(Entity<CEProductionRestComponent> ent, bool resting)
    {
        if (!TryComp<CEGOAPComponent>(ent, out var goap))
            return;
        goap.WorldState[ent.Comp.StateKey] = resting;
        goap.NextPlanTime = TimeSpan.Zero;
    }

    private void Release(Entity<CEProductionRestComponent> ent)
    {
        if (ent.Comp.Host is { } host && TryComp<CEProductionRestHostComponent>(host, out var restHost))
        {
            restHost.Residents.Remove(ent.Owner);
            if (!TerminatingOrDeleted(host))
                UpdateAvailability((host, restHost));
        }
        ent.Comp.Host = null;
        SetState(ent, false);
    }

    private void OnShutdown(Entity<CEProductionRestComponent> ent, ref ComponentShutdown args) => Release(ent);

    protected override void OnActionStartup(Entity<CEGOAPComponent> ent, ref CEGOAPActionStartupEvent<CEGOAPRestAtProductionHostAction> args)
    {
        // Production already requires contact. Stop native steering instead of introducing another movement owner.
        _steering.Unregister(ent);
    }

    protected override void OnActionUpdate(Entity<CEGOAPComponent> ent, ref CEGOAPActionUpdateEvent<CEGOAPRestAtProductionHostAction> args)
    {
        if (!TryComp<CEProductionRestComponent>(ent, out var rest) || rest.Host is not { } host ||
            !Exists(host) || _timing.CurTime >= rest.Until ||
            !_conditions.TryConditions(ent.Owner, rest.Conditions) ||
            !Transform(ent).Coordinates.TryDistance(EntityManager, Transform(host).Coordinates, out var distance) || distance > rest.HostRange)
        {
            args.Status = CEGOAPActionStatus.Finished;
        }
    }

    protected override void OnActionShutdown(Entity<CEGOAPComponent> ent, ref CEGOAPActionShutdownEvent<CEGOAPRestAtProductionHostAction> args)
    {
        if (TryComp<CEProductionRestComponent>(ent, out var rest))
            Release((ent, rest));
    }
}
