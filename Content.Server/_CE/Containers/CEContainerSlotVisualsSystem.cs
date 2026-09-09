using Content.Shared._CE.Containers;
using Robust.Shared.Containers;

namespace Content.Server._CE.Containers;

/// <summary>Displays existing container occupants without changing their physical transforms.</summary>
public sealed partial class CEContainerSlotVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CEContainerSlotVisualsComponent> ent, ref ComponentStartup args) => Refresh(ent);

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEContainerSlotVisualsComponent> ent, ref MapInitEvent args) => Refresh(ent);

    private void Refresh(Entity<CEContainerSlotVisualsComponent> ent)
    {
        foreach (var (id, visual) in ent.Comp.Slots)
        {
            if (!_containers.TryGetContainer(ent, id, out var container))
                continue;

            container.ShowContents = true;
            container.OccludesLight = false;
            foreach (var occupant in container.ContainedEntities)
                Apply(occupant, visual);
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CEContainerSlotVisualsComponent> ent, ref ComponentShutdown args)
    {
        foreach (var id in ent.Comp.Slots.Keys)
        {
            if (!_containers.TryGetContainer(ent, id, out var container))
                continue;
            foreach (var occupant in container.ContainedEntities)
                Clear(occupant);
        }
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<CEContainerSlotVisualsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.Owner != ent.Owner || !ent.Comp.Slots.TryGetValue(args.Container.ID, out var visual))
            return;

        args.Container.ShowContents = true;
        args.Container.OccludesLight = false;
        Apply(args.Entity, visual);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CEContainerSlotVisualsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Removal is reported before reparenting; the message identifies the slot being left.
        if (args.Container.Owner == ent.Owner && ent.Comp.Slots.ContainsKey(args.Container.ID))
            Clear(args.Entity);
    }

    private void Apply(EntityUid occupant, CEContainerSlotVisual visual)
    {
        var appearance = EnsureComp<AppearanceComponent>(occupant);
        _appearance.SetData(occupant, CEContainerSlotVisuals.Offset, visual.Offset, appearance);
        _appearance.SetData(occupant, CEContainerSlotVisuals.Rotation, visual.Rotation, appearance);
        _appearance.SetData(occupant, CEContainerSlotVisuals.Active, true, appearance);
    }

    private void Clear(EntityUid occupant)
    {
        if (TryComp<AppearanceComponent>(occupant, out var appearance))
            _appearance.SetData(occupant, CEContainerSlotVisuals.Active, false, appearance);
    }
}
