using Content.Shared._CE.Vehicle;
using Content.Shared._CE.Vehicle.Components;
using Content.Shared._CE.ZLevels.Flight;
using Content.Shared._CE.ZLevels.Flight.Components;
using Content.Shared.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.Stunnable;

namespace Content.Shared._CE.VehicleFlying;

public sealed class CEVehicleFlightSystem : EntitySystem
{
    [Dependency] private readonly CESharedZFlightSystem _flight = default!;
    [Dependency] private readonly CEVehicleSystem _vehicle = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEVehicleFlyerComponent, CEVehicleOperatorSetEvent>(OnOperatorSet);
        SubscribeLocalEvent<CEVehicleFlyerComponent, CEFlightStoppedEvent>(OnFlightStop);

        SubscribeLocalEvent<CEVehicleThrowOperatorOnDamageComponent, DamageChangedEvent>(OnTakeDamage);
    }

    private void OnFlightStop(Entity<CEVehicleFlyerComponent> ent, ref CEFlightStoppedEvent args)
    {
        if (!TryComp<CEVehicleComponent>(ent, out var vehicle))
            return;
        _vehicle.TryRemoveOperator((ent, vehicle));
    }

    private void OnTakeDamage(Entity<CEVehicleThrowOperatorOnDamageComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (!TryComp<CEVehicleComponent>(ent.Owner, out var vehicleComp))
            return;

        if (vehicleComp.Operator is null)
            return;

        _stun.TryKnockdown(vehicleComp.Operator.Value, ent.Comp.StunTime);
        _vehicle.TryRemoveOperator((ent, vehicleComp));
    }

    private void OnOperatorSet(Entity<CEVehicleFlyerComponent> ent, ref CEVehicleOperatorSetEvent args)
    {
        if (!TryComp<CEZFlyerComponent>(ent.Owner, out var flyerComp))
            return;

        if (args.NewOperator is null)
        {
            _flight.DeactivateFlight(ent.Owner);

            if (args.OldOperator is not null)
                RemoveFlightActionsFromOperator((ent, flyerComp), args.OldOperator.Value);
        }
        else
        {
            GrantFlightActionsToOperator((ent, flyerComp), args.NewOperator.Value);
            _flight.TryActivateFlight(ent.Owner);
        }
    }

    private void GrantFlightActionsToOperator(Entity<CEZFlyerComponent> flyer, EntityUid user)
    {
        List<EntityUid> actionsList = new();

        if (flyer.Comp.ZLevelDownActionEntity is not null)
            actionsList.Add(flyer.Comp.ZLevelDownActionEntity.Value);
        if (flyer.Comp.ZLevelUpActionEntity is not null)
            actionsList.Add(flyer.Comp.ZLevelUpActionEntity.Value);

        _actions.GrantActions(user, actionsList, flyer.Owner);
    }

    private void RemoveFlightActionsFromOperator(Entity<CEZFlyerComponent> flyer, EntityUid user)
    {
        if (flyer.Comp.ZLevelUpActionEntity is not null)
            _actions.RemoveProvidedAction(user, flyer.Owner, flyer.Comp.ZLevelUpActionEntity.Value);
        if (flyer.Comp.ZLevelDownActionEntity is not null)
            _actions.RemoveProvidedAction(user, flyer.Owner, flyer.Comp.ZLevelDownActionEntity.Value);
    }
}
