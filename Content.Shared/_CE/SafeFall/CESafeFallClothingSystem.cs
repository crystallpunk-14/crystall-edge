using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._CE.SafeFall;

public sealed partial class CESafeFallClothingSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffect = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private EntityQuery<CEZPhysicsComponent> _zPhysicsQuery = default!;
    [Dependency] private EntityQuery<BatteryComponent> _batteryQuery = default!;

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<CESafeFallClothingComponent> ent, ref GotEquippedEvent args)
    {
        _zLevels.RefreshVelocityRaiseEvent(args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnUnequipped(Entity<CESafeFallClothingComponent> ent, ref GotUnequippedEvent args)
    {
        _zLevels.RefreshVelocityRaiseEvent(args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnRefreshVelocityRaise(Entity<CESafeFallClothingComponent> ent, ref InventoryRelayedEvent<CERefreshVelocityRaiseEvent> args)
    {
        args.Args.Raise = true;
    }

    [SubscribeLocalEvent]
    private void OnGetZVelocity(Entity<CESafeFallClothingComponent> ent, ref InventoryRelayedEvent<CEGetZVelocityEvent> args)
    {
        var target = args.Args.Target;
        if (!target.Comp.Fallable || target.Comp.Velocity > -ent.Comp.VelocityThreshold)
            return;

        if (!HasCharge(ent))
            return;

        _zLevels.SetZVelocity((target.Owner, target.Comp), target.Comp.Velocity * ent.Comp.VelocityMultiplier);

        if (_net.IsServer)
            Activate(ent, target.Owner);
    }

    [SubscribeLocalEvent]
    private void OnZLevelRelayedFall(Entity<CESafeFallClothingComponent> ent, ref InventoryRelayedEvent<CEZLevelChasmAttempt> args)
    {
        OnZLevelFall(ent, ref args.Args);
    }

    [SubscribeLocalEvent]
    private void OnZLevelFall(Entity<CESafeFallClothingComponent> ent, ref CEZLevelChasmAttempt args)
    {
        if (args.Cancelled)
            return;

        // An active amulet status catches the wearer by itself, without spending charge
        if (_statusEffect.HasStatusEffect(args.Falled, ent.Comp.StatusEffect))
            return;

        if (!HasCharge(ent))
            return;

        args.Cancel();

        if (_zPhysicsQuery.TryComp(args.Falled, out var zPhysics))
        {
            _zLevels.SetZPosition((args.Falled, zPhysics), 0f);
            if (zPhysics.Velocity < 0)
                _zLevels.SetZVelocity((args.Falled, zPhysics), 0f);
        }

        if (_net.IsServer)
            Activate(ent, args.Falled);
    }

    private bool HasCharge(Entity<CESafeFallClothingComponent> ent)
    {
        return _batteryQuery.TryComp(ent, out var battery) && _battery.GetCharge((ent, battery)) >= ent.Comp.ChargeCost;
    }

    private void Activate(Entity<CESafeFallClothingComponent> ent, EntityUid wearer)
    {
        if (!_statusEffect.TrySetStatusEffectDuration(wearer, ent.Comp.StatusEffect, ent.Comp.Duration))
            return;

        _battery.UseCharge(ent.Owner, ent.Comp.ChargeCost);
        _stun.TryKnockdown(wearer, ent.Comp.StunDuration);

        _popup.PopupEntity(Loc.GetString("ce-zlevels-safe-fall-amulet-activate", ("name", MetaData(ent).EntityName)), wearer, PopupType.MediumCaution);
        _audio.PlayPvs(ent.Comp.Sound, wearer);
    }
}
