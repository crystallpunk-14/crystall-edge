using Content.Shared._CE.ThirdArm.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared._CE.ThirdArm;

public abstract partial class CESharedThirdArmSystem
{
    [Dependency] protected SharedBatterySystem Battery = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;

    private void InitBatteryModule()
    {
        SubscribeLocalEvent<CEThirdArmComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<CEThirdArmComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
        SubscribeLocalEvent<CEThirdArmActionManaCostComponent, ActionAttemptEvent>(OnActionManaCostAttempt);
    }

    /// <summary>
    ///     Generic mana cost for any action granted by a third arm module (see
    ///     CEThirdArmModuleActionsGrantComponent). Placed on the ACTION entity itself and gated/charged here,
    ///     before the action's own event fires - so any module's action just needs this component, no
    ///     per-action mana-check code.
    /// </summary>
    private void OnActionManaCostAttempt(Entity<CEThirdArmActionManaCostComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<ActionComponent>(ent, out var action) || action.Container is not { } module)
            return;

        if (!Container.TryGetContainingContainer((module, null, null), out var armContainer))
            return;

        var arm = armContainer.Owner;

        if (Battery.GetCharge(arm) < ent.Comp.ManaCost)
        {
            Popup.PopupClient(Loc.GetString("ce-magic-spell-not-enough-mana"), args.User, args.User);
            args.Cancelled = true;
            return;
        }

        Battery.TryUseCharge(arm, ent.Comp.ManaCost);
    }

    private void OnRefreshChargeRate(Entity<CEThirdArmComponent> ent, ref RefreshChargeRateEvent args)
    {
        var module = ent.Comp.ModuleSlot.Item;
        if (module == null || !TryComp<CEThirdArmModuleComponent>(module.Value, out var moduleComp))
            return;

        args.NewChargeRate -= moduleComp.PassiveDrainRate;
    }

    private void OnBatteryStateChanged(Entity<CEThirdArmComponent> ent, ref BatteryStateChangedEvent args)
    {
        var module = ent.Comp.ModuleSlot.Item;
        if (module == null)
            return;

        SetModulePowered(module.Value, args.NewState != BatteryState.Empty);
    }

    /// <summary>
    ///     Recalculates the arm's charge rate and the currently inserted module's power state from scratch.
    ///     Call whenever the inserted module (and thus its passive drain) changes.
    /// </summary>
    private void UpdateModulePower(Entity<CEThirdArmComponent> ent)
    {
        Battery.RefreshChargeRate(ent.Owner);

        var module = ent.Comp.ModuleSlot.Item;
        if (module == null)
            return;

        if (!TryComp<CEThirdArmModuleComponent>(module.Value, out var moduleComp))
            return;

        var hasPower = moduleComp.PassiveDrainRate <= 0f || Battery.GetCharge(ent.Owner) > 0f;
        SetModulePowered(module.Value, hasPower);
    }

    private void SetModulePowered(EntityUid module, bool powered)
    {
        if (powered)
        {
            var poweredEvent = new CEThirdArmModulePoweredEvent();
            RaiseLocalEvent(module, ref poweredEvent);
        }
        else
        {
            var unpoweredEvent = new CEThirdArmModuleUnpoweredEvent();
            RaiseLocalEvent(module, ref unpoweredEvent);
        }
    }
}

/// <summary>
///     Raised on a module entity when it has power available (inserted, and either drains nothing or the
///     arm's battery has charge).
/// </summary>
[ByRefEvent]
public readonly record struct CEThirdArmModulePoweredEvent;

/// <summary>
///     Raised on a module entity when it loses power (removed from the arm, or the arm's battery ran out).
/// </summary>
[ByRefEvent]
public readonly record struct CEThirdArmModuleUnpoweredEvent;
