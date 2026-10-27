using Content.Shared._CE.LockKey.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.LockKey;

public abstract partial class CESharedLockKeySystem
{
    [SubscribeLocalEvent]
    private void OnLockpickInteract(Entity<CELockpickComponent> lockpick, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!_ceLockQuery.TryComp(target, out var ceLock) || ceLock.Shape.Count == 0)
            return;

        if (!_lockQuery.TryComp(target, out var lockComp) || !lockComp.Locked)
            return;

        args.Handled = true;

        if (ceLock.Picker is { } picker && _ui.IsUiOpen(target, CELockpickUiKey.Key, picker))
        {
            if (picker != args.User)
                _popup.PopupClient(Loc.GetString("ce-lock-lock-pick-busy"), target, args.User);

            return;
        }

        SetPicking((target, ceLock), args.User, lockpick);
        _ui.OpenUi(target, CELockpickUiKey.Key, args.User, predicted: true);
    }

    [SubscribeLocalEvent]
    private void OnLockpickPress(Entity<CELockComponent> ent, ref CELockpickPressMessage args)
    {
        if (args.Actor != ent.Comp.Picker || ent.Comp.Lockpick is not { } lockpick)
            return;

        if (args.Power is < MinHeight or > MaxHeight)
            return;

        if (!_hands.IsHolding(args.Actor, lockpick) || !TryComp<CELockpickComponent>(lockpick, out var lockpickComp))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.Actor,
            lockpickComp.HackTime,
            new CELockpickHackDoAfterEvent(args.Power),
            ent,
            ent,
            lockpick)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            CancelDuplicate = false,
        });
    }

    [SubscribeLocalEvent]
    private void OnLockHacked(Entity<CELockComponent> ent, ref CELockpickHackDoAfterEvent args)
    {
        // No IsFirstTimePredicted gate: state must be reapplied on every resimulation, otherwise the client rolls it back.
        if (args.Handled)
            return;

        if (!TryComp<CELockpickComponent>(args.Used, out var lockpick) || ent.Comp.LockPickStatus >= ent.Comp.Shape.Count)
            return;

        args.Handled = true;

        // Any interrupted press counts as a failure.
        if (!args.Cancelled && args.Power == ent.Comp.Shape[ent.Comp.LockPickStatus])
        {
            _audio.PlayPredicted(lockpick.SuccessSound, ent, args.User);
            ent.Comp.LockPickStatus++;

            // On unlock the window closes and resets; if unlocking failed, start over.
            if (ent.Comp.LockPickStatus >= ent.Comp.Shape.Count && !_lock.TryUnlock(ent, args.User))
                ent.Comp.LockPickStatus = 0;

            DirtyField(ent, ent.Comp, nameof(CELockComponent.LockPickStatus));
            return;
        }

        _audio.PlayPredicted(lockpick.FailSound, ent, args.User);
        ent.Comp.LockPickStatus = 0;
        ent.Comp.LockPickFailures++;
        DirtyFields(ent, ent.Comp, null,
            nameof(CELockComponent.LockPickStatus),
            nameof(CELockComponent.LockPickFailures));
    }

    [SubscribeLocalEvent]
    private void OnLockpickUiClosed(Entity<CELockComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, CELockpickUiKey.Key) || args.Actor != ent.Comp.Picker)
            return;

        // Closing the window mid-press fails the press.
        CancelHackDoAfter(args.Actor, ent);
        SetPicking(ent, null, null);
    }

    [SubscribeLocalEvent]
    private void OnLockToggled(Entity<CELockComponent> ent, ref LockToggledEvent args)
    {
        if (!args.Locked)
            _ui.CloseUi(ent.Owner, CELockpickUiKey.Key);
    }

    [SubscribeLocalEvent]
    private void OnLockpickUnequipped(Entity<CELockpickComponent> ent, ref GotUnequippedHandEvent args)
    {
        var query = EntityQueryEnumerator<CELockComponent>();
        while (query.MoveNext(out var uid, out var ceLock))
        {
            if (ceLock.Lockpick != ent.Owner)
                continue;

            _ui.CloseUi(uid, CELockpickUiKey.Key, args.User);
            return;
        }
    }

    private void SetPicking(Entity<CELockComponent> ent, EntityUid? picker, EntityUid? lockpick)
    {
        ent.Comp.Picker = picker;
        ent.Comp.Lockpick = lockpick;
        ent.Comp.LockPickStatus = 0;
        DirtyFields(ent, ent.Comp, null,
            nameof(CELockComponent.Picker),
            nameof(CELockComponent.Lockpick),
            nameof(CELockComponent.LockPickStatus));
    }

    private void CancelHackDoAfter(EntityUid user, EntityUid lockEnt)
    {
        if (!TryComp<DoAfterComponent>(user, out var doAfters))
            return;

        foreach (var doAfter in doAfters.DoAfters.Values)
        {
            if (doAfter.Cancelled || doAfter.Completed)
                continue;

            if (doAfter.Args.Event is not CELockpickHackDoAfterEvent || doAfter.Args.Target != lockEnt)
                continue;

            _doAfter.Cancel(user, doAfter.Index, doAfters);
            return;
        }
    }
}

[Serializable, NetSerializable]
public enum CELockpickUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CELockpickPressMessage(int power) : BoundUserInterfaceMessage
{
    public readonly int Power = power;
}

[Serializable, NetSerializable]
public sealed partial class CELockpickHackDoAfterEvent : DoAfterEvent
{
    [DataField]
    public int Power;

    public CELockpickHackDoAfterEvent(int power)
    {
        Power = power;
    }

    public override DoAfterEvent Clone() => this;
}
