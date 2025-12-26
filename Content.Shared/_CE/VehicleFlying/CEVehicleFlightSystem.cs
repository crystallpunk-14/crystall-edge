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
        SubscribeLocalEvent<CEVehicleThrowOperatorOnDamageComponent, DamageChangedEvent>(OnTakeDamage);
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
            {
                if (flyerComp.ZLevelUpActionEntity is not null)
                    _actions.RemoveProvidedAction(args.OldOperator.Value, ent.Owner, flyerComp.ZLevelUpActionEntity.Value);
                if (flyerComp.ZLevelDownActionEntity is not null)
                    _actions.RemoveProvidedAction(args.OldOperator.Value, ent.Owner, flyerComp.ZLevelDownActionEntity.Value);
            }
        }
        else
        {
            List<EntityUid> actionsList = new();

            if (flyerComp.ZLevelDownActionEntity is not null)
                actionsList.Add(flyerComp.ZLevelDownActionEntity.Value);
            if (flyerComp.ZLevelUpActionEntity is not null)
                actionsList.Add(flyerComp.ZLevelUpActionEntity.Value);

            _actions.GrantActions(args.NewOperator.Value, actionsList, ent.Owner);
            _flight.TryActivateFlight(ent.Owner);
        }
    }
}
