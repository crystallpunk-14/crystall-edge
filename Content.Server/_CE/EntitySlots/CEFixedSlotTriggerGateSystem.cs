using Content.Shared.Trigger;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Content.Shared.Whitelist;

namespace Content.Server._CE.EntitySlots;

/// <summary>
/// Gates a timer trigger by validated fixed-slot membership without owning the trigger lifecycle itself.
/// </summary>
public sealed partial class CEFixedSlotTriggerGateSystem : EntitySystem
{
    [Dependency] private CEFixedEntitySlotSystem _fixedSlots = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEFixedSlotTriggerGateComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEFixedSlotTriggerGateComponent, CEFixedEntitySlotInsertedEvent>(OnInserted);
        SubscribeLocalEvent<CEFixedSlotTriggerGateComponent, CEFixedEntitySlotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<CEFixedSlotTriggerGateComponent, AttemptTriggerEvent>(OnAttemptTrigger);
        SubscribeLocalEvent<CEFixedSlotTriggerGateComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<CEFixedSlotTriggerGateComponent> ent, ref MapInitEvent args)
    {
        SynchronizeTimer(ent);
    }

    private void OnInserted(
        Entity<CEFixedSlotTriggerGateComponent> ent,
        ref CEFixedEntitySlotInsertedEvent args)
    {
        SynchronizeTimer(ent);
    }

    private void OnRemoved(
        Entity<CEFixedSlotTriggerGateComponent> ent,
        ref CEFixedEntitySlotRemovedEvent args)
    {
        PauseTimer(ent.Owner);
    }

    private void OnAttemptTrigger(
        Entity<CEFixedSlotTriggerGateComponent> ent,
        ref AttemptTriggerEvent args)
    {
        if (!TryComp<TimerTriggerComponent>(ent.Owner, out var timer) || HasValidHost(ent))
            return;

        // A null key activates every trigger effect, including both the timer and its output.
        // Also reject the timer's explicit input/output keys while fixed-slot membership is invalid.
        if (args.Key != null &&
            !string.Equals(args.Key, timer.KeyOut, StringComparison.Ordinal) &&
            !timer.KeysIn.Contains(args.Key))
            return;

        PauseTimer(ent.Owner);
        args.Cancelled = true;
    }

    private void OnShutdown(Entity<CEFixedSlotTriggerGateComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Owner))
            PauseTimer(ent.Owner);
    }

    private void SynchronizeTimer(Entity<CEFixedSlotTriggerGateComponent> ent)
    {
        if (!HasValidHost(ent))
        {
            PauseTimer(ent.Owner);
            return;
        }

        if (HasComp<ActiveTimerTriggerComponent>(ent.Owner) ||
            !TryComp<TimerTriggerComponent>(ent.Owner, out var timer))
            return;

        _trigger.ActivateTimerTrigger((ent.Owner, timer));
    }

    private void PauseTimer(EntityUid uid)
    {
        if (!TryComp<TimerTriggerComponent>(uid, out var timer) ||
            !HasComp<ActiveTimerTriggerComponent>(uid))
            return;

        var remaining = _trigger.GetRemainingTime((uid, timer));
        if (remaining is { } delay)
            _trigger.SetDelay((uid, timer), delay > TimeSpan.Zero ? delay : TimeSpan.Zero);

        _trigger.StopTimerTrigger((uid, timer));
    }

    private bool HasValidHost(Entity<CEFixedSlotTriggerGateComponent> ent)
    {
        return _fixedSlots.TryGetSlot(ent.Owner, out var host, out _) &&
            _whitelist.IsWhitelistPass(ent.Comp.HostWhitelist, host);
    }
}
