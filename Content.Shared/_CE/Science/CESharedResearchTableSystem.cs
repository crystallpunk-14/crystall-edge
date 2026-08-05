using Content.Shared._CE.Science.Components;
using Robust.Shared.Containers;

namespace Content.Shared._CE.Science;

public abstract partial class CESharedResearchTableSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEResearchTableComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<CEResearchTableComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
    }

    private void OnContainerChanged(Entity<CEResearchTableComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.PaperSlotId)
            return;

        _appearance.SetData(ent.Owner, CEResearchTableVisuals.HasPaper, true);
        OnPaperStateChanged(ent);
    }

    private void OnContainerChanged(Entity<CEResearchTableComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.PaperSlotId)
            return;

        _appearance.SetData(ent.Owner, CEResearchTableVisuals.HasPaper, false);
        OnPaperStateChanged(ent);
    }

    protected virtual void OnPaperStateChanged(Entity<CEResearchTableComponent> ent)
    {
    }
}
