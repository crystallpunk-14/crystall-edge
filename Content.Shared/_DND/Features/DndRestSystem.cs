using Content.Shared._DND.Classes;
using Content.Shared._DND.Spells;
using Content.Shared.Bed.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;

namespace Content.Shared._DND.Features;

/// <summary>
/// Short rests happen at a bonfire or anything you can buckle to. Long rests happen at a bed. The weapon drill follows a long rest.
/// </summary>
public sealed partial class DndRestSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private DndRageSystem _rage = default!;
    [Dependency] private DndInspirationSystem _inspiration = default!;
    [Dependency] private DndMasterySystem _mastery = default!;
    [Dependency] private DndSpellSystem _spells = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnBonfireVerbs(EntityUid uid, DndBonfireComponent component, GetVerbsEvent<Verb> args)
    {
        AddShortRest(args);
    }

    [SubscribeLocalEvent]
    private void OnSeatVerbs(EntityUid uid, StrapComponent component, GetVerbsEvent<Verb> args)
    {
        AddShortRest(args);
    }

    [SubscribeLocalEvent]
    private void OnBedVerbs(EntityUid uid, HealOnBuckleComponent component, GetVerbsEvent<Verb> args)
    {
        AddLongRest(args);
        _spells.AddPrepareVerbs(args);
    }

    [SubscribeLocalEvent]
    private void OnShortRest(EntityUid uid, DndClassComponent component, DndShortRestEvent args)
    {
        if (!_net.IsServer || args.Cancelled)
            return;

        if (TryComp<DndRageComponent>(uid, out var rage))
            _rage.RecoverShortRest(uid, rage);

        _popup.PopupEntity(Loc.GetString("dnd-rest-short-done"), uid, uid, PopupType.Medium);
    }

    [SubscribeLocalEvent]
    private void OnLongRest(EntityUid uid, DndClassComponent component, DndLongRestEvent args)
    {
        if (!_net.IsServer || args.Cancelled)
            return;

        if (TryComp<DndRageComponent>(uid, out var rage))
            _rage.RecoverLongRest(uid, rage);

        if (TryComp<DndWeaponMasteryComponent>(uid, out var mastery))
        {
            mastery.SwapsRemaining = 1;
            if (mastery.Drill is { } drill)
                _charges.SetCharges(drill, 1);
        }

        if (TryComp<DndBardicInspirationSourceComponent>(uid, out var inspiration))
            _inspiration.RecoverLongRest(uid, inspiration);

        if (TryComp<DndSpellcastingComponent>(uid, out var casting))
            _spells.RecoverLongRest(uid, casting);

        _popup.PopupEntity(Loc.GetString("dnd-rest-long-done"), uid, uid, PopupType.Medium);
    }

    [SubscribeLocalEvent]
    private void OnDrill(EntityUid uid, DndWeaponMasteryComponent component, DndWeaponDrillActionEvent args)
    {
        if (!_net.IsServer)
            return;

        args.Handled = _mastery.SwapOne(uid, component);
    }

    private void AddShortRest(GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !HasComp<DndClassComponent>(args.User))
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("dnd-rest-short-verb"),
            Priority = 20,
            Act = () => BeginRest(args.User, DndRest.Short, new DndShortRestEvent(), "dnd-rest-short-start"),
        });
    }

    private void AddLongRest(GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !HasComp<DndClassComponent>(args.User))
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("dnd-rest-long-verb"),
            Priority = 19,
            Act = () => BeginRest(args.User, DndRest.Long, new DndLongRestEvent(), "dnd-rest-long-start"),
        });
    }

    private void BeginRest(EntityUid uid, TimeSpan delay, DoAfterEvent doAfter, string message)
    {
        if (!_net.IsServer)
            return;

        if (TryComp<DndRageComponent>(uid, out var rage))
            _rage.EndRage(uid, rage, tell: false);

        var started = new DoAfterArgs(EntityManager, uid, delay, doAfter, uid)
        {
            BreakOnDamage = true,
            BreakOnMove = false,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(started))
            _popup.PopupEntity(Loc.GetString(message), uid, uid, PopupType.Medium);
    }
}
