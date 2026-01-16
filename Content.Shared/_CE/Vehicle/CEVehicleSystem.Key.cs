using Content.Shared._CE.Vehicle.Components;
using Robust.Shared.Containers;

namespace Content.Shared._CE.Vehicle;

public sealed partial class CEVehicleSystem
{
    public void InitializeKey()
    {
        SubscribeLocalEvent<CEGenericKeyedVehicleComponent, ContainerIsInsertingAttemptEvent>(OnGenericKeyedInsertAttempt);
        SubscribeLocalEvent<CEGenericKeyedVehicleComponent, EntInsertedIntoContainerMessage>(OnGenericKeyedEntInserted);
        SubscribeLocalEvent<CEGenericKeyedVehicleComponent, EntRemovedFromContainerMessage>(OnGenericKeyedEntRemoved);
        SubscribeLocalEvent<CEGenericKeyedVehicleComponent, CEVehicleCanRunEvent>(OnGenericKeyedCanRun);
    }

    private void OnGenericKeyedInsertAttempt(Entity<CEGenericKeyedVehicleComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.PreventInvalidInsertion || args.Container.ID != ent.Comp.ContainerId)
            return;

        if (_entityWhitelist.IsWhitelistPass(ent.Comp.KeyWhitelist, args.EntityUid))
            return;

        args.Cancel();
    }

    private void OnGenericKeyedEntInserted(Entity<CEGenericKeyedVehicleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;
        RefreshCanRun(ent.Owner);
    }

    private void OnGenericKeyedEntRemoved(Entity<CEGenericKeyedVehicleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;
        RefreshCanRun(ent.Owner);
    }

    private void OnGenericKeyedCanRun(Entity<CEGenericKeyedVehicleComponent> ent, ref CEVehicleCanRunEvent args)
    {
        if (!args.CanRun)
            return;
        // We cannot run by default
        args.CanRun = false;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return;

        foreach (var contained in container.ContainedEntities)
        {
            if (_entityWhitelist.IsWhitelistFail(ent.Comp.KeyWhitelist, contained))
                continue;

            // If we find a valid key, permit running and exit early.
            args.CanRun = true;
            break;
        }
    }
}
