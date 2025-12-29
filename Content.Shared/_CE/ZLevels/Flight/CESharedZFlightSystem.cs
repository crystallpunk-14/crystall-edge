/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared._CE.ZLevels.Flight.Components;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Stunnable;
using Content.Shared.Toggleable;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.ZLevels.Flight;

public abstract class CESharedZFlightSystem : EntitySystem
{
    [Dependency] private readonly CESharedZLevelsSystem _zLevel = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    protected EntityQuery<CEZPhysicsComponent> ZPhyzQuery;

    public override void Initialize()
    {
        base.Initialize();

        ZPhyzQuery = GetEntityQuery<CEZPhysicsComponent>();

        SubscribeLocalEvent<CEZPhysicsComponent, CEFlightStartedEvent>(OnStartFlight);
        SubscribeLocalEvent<CEZPhysicsComponent, CEFlightStoppedEvent>(OnStopFlight);
        SubscribeLocalEvent<CEZFlyerComponent, CEGetZVelocityEvent>(OnGetZVelocity);

        SubscribeLocalEvent<CEZFlyerComponent, CEZFlightActionUp>(OnZLevelUp);
        SubscribeLocalEvent<CEZFlyerComponent, CEZFlightActionDown>(OnZLevelDown);
        SubscribeLocalEvent<CEZFlyerComponent, ToggleActionEvent>(OnZLevelToggle);
        SubscribeLocalEvent<CEZFlyerComponent, CEStartFlightDoAfterEvent>(OnStartFlightDoAfter);

        SubscribeLocalEvent<CEZFlyerComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<CEZFlyerComponent, KnockedDownEvent>(OnKnockDowned);
        SubscribeLocalEvent<CEZFlyerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CEZFlyerComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<CEZFlyerComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (!args.InterruptsDoAfters)
            return;

        DeactivateFlight((ent, ent));
    }

    private void OnMobStateChanged(Entity<CEZFlyerComponent> ent, ref MobStateChangedEvent args)
    {
        DeactivateFlight((ent, ent));
    }

    private void OnKnockDowned(Entity<CEZFlyerComponent> ent, ref KnockedDownEvent args)
    {
        DeactivateFlight((ent, ent));
    }

    private void OnStunned(Entity<CEZFlyerComponent> ent, ref StunnedEvent args)
    {
        DeactivateFlight((ent, ent));
    }

    private void OnStartFlight(Entity<CEZPhysicsComponent> ent, ref CEFlightStartedEvent args)
    {
        if (!TryComp<CEZFlyerComponent>(ent, out var flyerComp))
            return;
        SetTargetHeight((ent, flyerComp), ent.Comp.CurrentZLevel);

        StartFlightVisuals((ent, flyerComp));

        _actions.SetEnabled(flyerComp.ZLevelDownActionEntity, true);
        _actions.SetEnabled(flyerComp.ZLevelUpActionEntity, true);
    }

    private void OnStopFlight(Entity<CEZPhysicsComponent> ent, ref CEFlightStoppedEvent args)
    {
        if (!TryComp<CEZFlyerComponent>(ent, out var flyerComp))
            return;

        StopFlightVisuals((ent, flyerComp));

        _actions.SetEnabled(flyerComp.ZLevelDownActionEntity, false);
        _actions.SetEnabled(flyerComp.ZLevelUpActionEntity, false);
    }

    private void OnGetZVelocity(Entity<CEZFlyerComponent> ent, ref CEGetZVelocityEvent args)
    {
        if (!ent.Comp.Active)
            return;

        var zPhys = args.Target.Comp;
        var currentPos = zPhys.CurrentZLevel + zPhys.LocalPosition;
        var targetPos = ent.Comp.TargetMapHeight + 0.5f;
        var currentVelocity = zPhys.Velocity;

        var distanceToTarget = targetPos - currentPos;

        var targetVelocity = Math.Clamp(distanceToTarget * ent.Comp.FlightSpeed, -ent.Comp.FlightSpeed, ent.Comp.FlightSpeed);
        var velocityDelta = targetVelocity - currentVelocity;

        var upperBound = ent.Comp.TargetMapHeight + 0.9f;
        var lowerBound = ent.Comp.TargetMapHeight + 0.1f;

        var newVelocity = currentVelocity + velocityDelta;
        var nextPos = currentPos + newVelocity;

        if (nextPos > upperBound)
        {
            var maxAllowedVelocity = upperBound - currentPos;
            velocityDelta = maxAllowedVelocity - currentVelocity;
        }
        else if (nextPos < lowerBound)
        {
            var maxAllowedVelocity = lowerBound - currentPos;
            velocityDelta = maxAllowedVelocity - currentVelocity;
        }

        args.VelocityDelta = velocityDelta;
    }

    private void OnZLevelUp(Entity<CEZFlyerComponent> ent, ref CEZFlightActionUp args)
    {
        if (args.Handled)
            return;

        var map = Transform(ent).MapUid;
        if (map is null)
            return;

        if (!_zLevel.TryMapUp(map.Value, out var mapAbove))
            return;

        ent.Comp.TargetMapHeight = mapAbove.Value.Comp.Depth;
        DirtyField(ent, ent.Comp, nameof(CEZFlyerComponent.TargetMapHeight));

        args.Handled = true;
    }

    private void OnZLevelDown(Entity<CEZFlyerComponent> ent, ref CEZFlightActionDown args)
    {
        if (args.Handled)
            return;

        var map = Transform(ent).MapUid;
        if (map is null)
            return;

        if (!_zLevel.TryMapDown(map.Value, out var mapBelow))
            return;

        ent.Comp.TargetMapHeight = mapBelow.Value.Comp.Depth;
        DirtyField(ent, ent.Comp, nameof(CEZFlyerComponent.TargetMapHeight));

        args.Handled = true;
    }

    private void OnZLevelToggle(Entity<CEZFlyerComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Active)
        {
            DeactivateFlight((ent, ent));
        }
        else
        {
            // If StartFlightDoAfter is set, start a doAfter before activating flight
            if (ent.Comp.StartFlightDoAfter != null)
            {
                //Preventive start flying visuals
                StartFlightVisuals(ent);

                var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.StartFlightDoAfter.Value, new CEStartFlightDoAfterEvent(), ent)
                {
                    BreakOnMove = false,
                    BlockDuplicate = true,
                    BreakOnDamage = true,
                    CancelDuplicate = true,
                };

                _doAfter.TryStartDoAfter(doAfter);
            }
            else
            {
                // No delay, activate flight immediately
                TryActivateFlight((ent, ent));
            }
        }

        args.Handled = true;
    }

    private void OnStartFlightDoAfter(Entity<CEZFlyerComponent> ent, ref CEStartFlightDoAfterEvent args)
    {

        if (args.Cancelled || args.Handled)
        {
            StopFlightVisuals(ent);
            return;
        }

        TryActivateFlight((ent, ent));
        args.Handled = true;
    }

    [PublicAPI]
    public bool TryActivateFlight(Entity<CEZFlyerComponent?> ent, CEZPhysicsComponent? zPhys = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!Resolve(ent, ref zPhys, false))
            return false;

        if (ent.Comp.Active)
            return false;

        var ev = new CEStartFlightAttemptEvent();
        RaiseLocalEvent(ent, ev);

        if (ev.Cancelled)
            return false;

        ent.Comp.Active = true;
        DirtyField(ent, ent.Comp, nameof(CEZFlyerComponent.Active));

        _zLevel.SetZGravity((ent, zPhys), 0);

        // Update toggle action icon state
        if (ent.Comp.ZLevelToggleActionEntity != null)
            _actions.SetToggled(ent.Comp.ZLevelToggleActionEntity, true);

        RaiseLocalEvent(ent, new CEFlightStartedEvent());
        return true;
    }

    [PublicAPI]
    public void DeactivateFlight(Entity<CEZFlyerComponent?> ent, CEZPhysicsComponent? zPhys = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!Resolve(ent, ref zPhys, false))
            return;

        if (!ent.Comp.Active)
            return;

        ent.Comp.Active = false;
        DirtyField(ent, ent.Comp, nameof(CEZFlyerComponent.Active));

        _zLevel.SetZGravity((ent, zPhys), ent.Comp.DefaultGravityIntensity);

        // Update toggle action icon state
        if (ent.Comp.ZLevelToggleActionEntity != null)
            _actions.SetToggled(ent.Comp.ZLevelToggleActionEntity, false);

        RaiseLocalEvent(ent, new CEFlightStoppedEvent());
    }

    [PublicAPI]
    public void SetTargetHeight(Entity<CEZFlyerComponent> ent, int targetHeight)
    {
        ent.Comp.TargetMapHeight = targetHeight;
        DirtyField(ent, ent.Comp, nameof(CEZFlyerComponent.TargetMapHeight));
    }

    private void StartFlightVisuals(Entity<CEZFlyerComponent> ent)
    {
        _appearance.SetData(ent, CEFlightVisuals.Active, true);
        _ambient.SetAmbience(ent, true);
    }

    private void StopFlightVisuals(Entity<CEZFlyerComponent> ent)
    {
        _appearance.SetData(ent, CEFlightVisuals.Active, false);
        _ambient.SetAmbience(ent, false);
    }
}

/// <summary>
/// Called on an entity when it attempts to start flight mode. Subscribe and cancel this event if you want to cancel your flight for any reason.
/// </summary>
public sealed class CEStartFlightAttemptEvent : CancellableEntityEventArgs;

/// <summary>
/// Called on an entity when it enters flight mode
/// </summary>
public sealed class CEFlightStartedEvent : EntityEventArgs;

/// <summary>
/// Called on an entity when it exits flight mode
/// </summary>
public sealed class CEFlightStoppedEvent : EntityEventArgs;


/// <summary>
/// Instant Action, raising the target flight level by 1
/// </summary>
public sealed partial class CEZFlightActionUp : InstantActionEvent
{
}

/// <summary>
/// Instant Action, lowering the target flight level by 1
/// </summary>
public sealed partial class CEZFlightActionDown : InstantActionEvent
{
}


[Serializable, NetSerializable]
public enum CEFlightVisuals
{
    Active,
}

/// <summary>
/// DoAfter event for starting flight with a delay
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CEStartFlightDoAfterEvent : SimpleDoAfterEvent
{
}
