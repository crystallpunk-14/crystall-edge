using Content.Shared.Actions.Events;
using Content.Shared._DND.Combat;
using Content.Shared._DND.Equipment;
using Content.Shared.Charges.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._DND.Features;

/// <summary>
/// Entering and keeping a rage. An attack roll against an enemy refreshes it. Heavy armor and incapacitation end it.
/// </summary>
public sealed partial class DndRageSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private DndArmorSystem _armor = default!;
    [Dependency] private MobStateSystem _mobs = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<DndRageComponent>();
        while (query.MoveNext(out var uid, out var rage))
        {
            if (!rage.Active)
                continue;

            if (!DndRageRules.StillGoing(_timing.CurTime, rage.ExpiresAt, rage.MaxEnd) || Incapacitated(uid) || _armor.WearingHeavyArmor(uid))
                EndRage(uid, rage);
        }

        var resolved = EntityQueryEnumerator<DndAttackDamageResolvedComponent>();
        while (resolved.MoveNext(out var uid, out _))
            RemComp<DndAttackDamageResolvedComponent>(uid);
    }

    [SubscribeLocalEvent(after: [typeof(SharedChargesSystem)])]
    private void OnRageAttempt(EntityUid uid, DndRageActionComponent component, ref ActionAttemptEvent args)
    {
        if (!args.Cancelled)
            return;

        if (!TryComp<DndRageComponent>(args.User, out var rage) || !rage.Active)
            return;

        args.Cancelled = false;
        args.Reason = null;
    }

    [SubscribeLocalEvent]
    private void OnRage(EntityUid uid, DndRageComponent component, DndRageActionEvent args)
    {
        if (!_net.IsServer)
            return;

        if (component.Active)
        {
            if (!DndRageRules.StillGoing(_timing.CurTime, component.ExpiresAt, component.MaxEnd) || _timing.CurTime >= component.MaxEnd)
            {
                EndRage(uid, component);
                return;
            }

            // The action spends a charge after this returns. Putting one back keeps an extension free.
            if (component.Action is { } extendAction)
                _charges.AddCharges(extendAction, 1);

            component.ExpiresAt = DndRageRules.RefreshExpiry(_timing.CurTime, component.MaxEnd);
            Dirty(uid, component);
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("dnd-rage-extend"), uid, uid, PopupType.Medium);
            return;
        }

        if (Incapacitated(uid))
            return;

        if (_armor.WearingHeavyArmor(uid))
        {
            _popup.PopupEntity(Loc.GetString("dnd-rage-heavy-armor"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var shown = ShownUses(component);
        if (shown <= 0)
        {
            _popup.PopupEntity(Loc.GetString("dnd-rage-spent"), uid, uid, PopupType.SmallCaution);
            return;
        }

        component.Uses = shown - 1;
        component.Active = true;
        component.StartedAt = _timing.CurTime;
        component.MaxEnd = _timing.CurTime + TimeSpan.FromSeconds(DndRageRules.MaxSeconds);
        component.ExpiresAt = DndRageRules.RefreshExpiry(_timing.CurTime, component.MaxEnd);
        Dirty(uid, component);
        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("dnd-rage-start", ("uses", component.Uses), ("max", component.MaxUses)), uid, uid, PopupType.Medium);
    }

    [SubscribeLocalEvent]
    private void OnAttack(EntityUid uid, DndRageComponent component, ref DndAttackResolvedEvent args)
    {
        if (!_net.IsServer || !component.Active)
            return;

        component.ExpiresAt = DndRageRules.RefreshExpiry(_timing.CurTime, component.MaxEnd);
        Dirty(uid, component);
    }

    [SubscribeLocalEvent]
    private void OnDamageModify(EntityUid uid, DndRageComponent component, ref DamageModifyEvent args)
    {
        if (HasComp<DndAttackDamageResolvedComponent>(uid))
        {
            RemComp<DndAttackDamageResolvedComponent>(uid);
            return;
        }

        if (!component.Active || !_net.IsServer)
            return;

        DndRageRules.HalvePhysical(args.Damage);
    }

    [SubscribeLocalEvent]
    private void OnExamine(EntityUid uid, DndRageComponent component, ref ExaminedEvent args)
    {
        var text = component.Active ? "dnd-examine-raging" : "dnd-examine-rage";
        args.PushText(Loc.GetString(text, ("uses", component.Uses), ("max", component.MaxUses), ("bonus", component.DamageBonus)));
    }

    public void RecoverShortRest(EntityUid uid, DndRageComponent rage)
    {
        if (rage.Uses >= rage.MaxUses)
            return;

        rage.Uses++;
        Dirty(uid, rage);
        SyncCharges(rage);
    }

    public void RecoverLongRest(EntityUid uid, DndRageComponent rage)
    {
        EndRage(uid, rage, tell: false);
        rage.Uses = rage.MaxUses;
        Dirty(uid, rage);
        SyncCharges(rage);
    }

    public void EndRage(EntityUid uid, DndRageComponent rage, bool tell = true)
    {
        if (!rage.Active)
            return;

        rage.Active = false;
        Dirty(uid, rage);
        if (tell && _net.IsServer)
            _popup.PopupEntity(Loc.GetString("dnd-rage-end"), uid, uid, PopupType.Medium);
    }

    private bool Incapacitated(EntityUid uid)
    {
        return _mobs.IsIncapacitated(uid) || HasComp<StunnedComponent>(uid);
    }

    private int ShownUses(DndRageComponent rage)
    {
        if (rage.Action is { } action)
        {
            var shown = _charges.GetCurrentCharges(action);
            if (shown >= 0)
                return shown;
        }

        return rage.Uses;
    }

    private void SyncCharges(DndRageComponent rage)
    {
        if (rage.Action is not { } action)
            return;

        _charges.SetMaxCharges(action, rage.MaxUses);
        _charges.SetCharges(action, rage.Uses);
    }
}
