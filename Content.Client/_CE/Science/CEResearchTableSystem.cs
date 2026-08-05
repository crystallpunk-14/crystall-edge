using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;

namespace Content.Client._CE.Science;

public sealed partial class CEResearchTableSystem : CESharedResearchTableSystem
{
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEUnselectedDiscoveryProjectComponent, AfterAutoHandleStateEvent>(OnProjectStateChanged);
    }

    protected override void OnPaperStateChanged(Entity<CEResearchTableComponent> ent)
    {
        base.OnPaperStateChanged(ent);

        UpdateOpenUi(ent.Owner);
    }

    private void OnProjectStateChanged(Entity<CEUnselectedDiscoveryProjectComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var parent = Transform(ent).ParentUid;
        if (HasComp<CEResearchTableComponent>(parent))
            UpdateOpenUi(parent);
    }

    private void UpdateOpenUi(EntityUid table)
    {
        if (_userInterface.TryGetOpenUi(table, CEResearchTableUiKey.Key, out var bui))
            bui.Update();
    }
}
