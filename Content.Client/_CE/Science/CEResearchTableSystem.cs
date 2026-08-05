using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client._CE.Science;

public sealed partial class CEResearchTableSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEResearchTableComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<CEResearchTableComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
    }

    private void OnContainerChanged(Entity<CEResearchTableComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.PaperSlotId)
            UpdateUi(ent);
    }

    private void OnContainerChanged(Entity<CEResearchTableComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.PaperSlotId)
            UpdateUi(ent);
    }

    private void UpdateUi(EntityUid uid)
    {
        if (_userInterface.TryGetOpenUi(uid, CEResearchTableUiKey.Key, out var bui))
            bui.Update();
    }
}
