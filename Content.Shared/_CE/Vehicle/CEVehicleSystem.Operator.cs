using Content.Shared._CE.Vehicle.Components;
using Content.Shared.Buckle.Components;
using Robust.Shared.Containers;

namespace Content.Shared._CE.Vehicle;

public sealed partial class CEVehicleSystem
{
    public void InitializeOperator()
    {
        SubscribeLocalEvent<CEStrapVehicleComponent, StrappedEvent>(OnVehicleStrapped);
        SubscribeLocalEvent<CEStrapVehicleComponent, UnstrappedEvent>(OnVehicleUnstrapped);

        SubscribeLocalEvent<CEContainerVehicleComponent, EntInsertedIntoContainerMessage>(OnContainerEntInserted);
        SubscribeLocalEvent<CEContainerVehicleComponent, EntRemovedFromContainerMessage>(OnContainerEntRemoved);
    }

    private void OnVehicleStrapped(Entity<CEStrapVehicleComponent> ent, ref StrappedEvent args)
    {
        if (!TryComp<CEVehicleComponent>(ent, out var vehicle))
            return;
        TrySetOperator((ent, vehicle), args.Buckle);
    }

    private void OnVehicleUnstrapped(Entity<CEStrapVehicleComponent> ent, ref UnstrappedEvent args)
    {
        if (!TryComp<CEVehicleComponent>(ent, out var vehicle))
            return;
        TrySetOperator((ent, vehicle), null);
    }

    private void OnContainerEntInserted(Entity<CEContainerVehicleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!TryComp<CEVehicleComponent>(ent, out var vehicle))
            return;

        TrySetOperator((ent, vehicle), args.Entity, removeExisting: false);
    }

    private void OnContainerEntRemoved(Entity<CEContainerVehicleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!TryComp<CEVehicleComponent>(ent, out var vehicle))
            return;

        if (vehicle.Operator != args.Entity)
            return;

        TryRemoveOperator((ent, vehicle));
    }
}
