using Content.Shared._CE.Vehicle;
using Content.Shared._CE.Vehicle.Components;
using Content.Shared._CE.ZLevels.Flight;
using Content.Shared.Damage.Systems;
using Content.Shared.Stunnable;

namespace Content.Shared._CE.VehicleFlying;

public sealed class CEVehicleFlightSystem : EntitySystem
{
    [Dependency] private readonly CESharedZFlightSystem _flight = default!;
    [Dependency] private readonly CEVehicleSystem _vehicle = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

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
        if (args.NewOperator is null)
            _flight.DeactivateFlight(ent.Owner);
        else
            _flight.TryActivateFlight(ent.Owner);
    }
}
