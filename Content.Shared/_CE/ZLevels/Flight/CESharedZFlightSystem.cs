using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared._CE.ZLevels.Flight.Components;
using Content.Shared.Actions;
using Content.Shared.Audio;

namespace Content.Shared._CE.ZLevels.Flight;

public abstract class CESharedZFlightSystem : EntitySystem
{
    [Dependency] private readonly CESharedZLevelsSystem _zLevel = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;

    protected EntityQuery<CEZPhysicsComponent> ZPhyzQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        ZPhyzQuery = GetEntityQuery<CEZPhysicsComponent>();

        SubscribeLocalEvent<CEZPhysicsComponent, CEFlightStartedEvent>(OnStartFlight);
        SubscribeLocalEvent<CEZPhysicsComponent, CEFlightStoppedEvent>(OnStopFlight);
        SubscribeLocalEvent<CEZFlyerComponent, CEGetZVelocityEvent>(OnGetZVelocity);

        SubscribeLocalEvent<CEZFlyerComponent, CEZFlightActionUp>(OnZLevelUp);
        SubscribeLocalEvent<CEZFlyerComponent, CEZFlightActionDown>(OnZLevelDown);
    }
    private void OnZLevelDown(Entity<CEZFlyerComponent> ent, ref CEZFlightActionDown args)
    {
        var map = Transform(ent).MapUid;
        if (map is null)
            return;

        if (!_zLevel.TryMapDown(map.Value, out var mapBelow))
            return;

        ent.Comp.TargetMapHeight = mapBelow.Value.Comp.Depth;
        DirtyField(ent, ent.Comp, nameof(CEZFlyerComponent.TargetMapHeight));
    }

    private void OnZLevelUp(Entity<CEZFlyerComponent> ent, ref CEZFlightActionUp args)
    {
        var map = Transform(ent).MapUid;
        if (map is null)
            return;

        if (!_zLevel.TryMapUp(map.Value, out var mapAbove))
            return;

        ent.Comp.TargetMapHeight = mapAbove.Value.Comp.Depth;
        DirtyField(ent, ent.Comp, nameof(CEZFlyerComponent.TargetMapHeight));
    }

    private void OnStartFlight(Entity<CEZPhysicsComponent> ent, ref CEFlightStartedEvent args)
    {
        if (!TryComp<CEZFlyerComponent>(ent, out var flyerComp))
            return;
        SetTargetHeight((ent,flyerComp), ent.Comp.CurrentZLevel);

        _ambient.SetAmbience(ent, true);
    }

    private void OnStopFlight(Entity<CEZPhysicsComponent> ent, ref CEFlightStoppedEvent args)
    {
        _ambient.SetAmbience(ent, false);
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

        RaiseLocalEvent(ent, new CEFlightStartedEvent());
        return true;
    }

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

        RaiseLocalEvent(ent, new CEFlightStoppedEvent());
    }

    public void SetTargetHeight(Entity<CEZFlyerComponent> ent, int targetHeight)
    {
        ent.Comp.TargetMapHeight = targetHeight;
        DirtyField(ent, ent.Comp, nameof(CEZFlyerComponent.TargetMapHeight));
    }

    public void AdjustTargetHeight(Entity<CEZFlyerComponent> ent, int heightDelta)
    {
        ent.Comp.TargetMapHeight += heightDelta;
        DirtyField(ent, ent.Comp, nameof(CEZFlyerComponent.TargetMapHeight));
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
///
/// </summary>
public sealed partial class CEZFlightActionUp : InstantActionEvent
{
}

/// <summary>
///
/// </summary>
public sealed partial class CEZFlightActionDown : InstantActionEvent
{
}
