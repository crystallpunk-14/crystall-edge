using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;

namespace Content.Client._CE.Science;

public sealed partial class CEResearchTableSystem : CESharedResearchTableSystem
{
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEUnselectedDiscoveryProjectComponent, AfterAutoHandleStateEvent>(OnDraftStateChanged);
        SubscribeLocalEvent<CEDiscoveryProjectComponent, AfterAutoHandleStateEvent>(OnActiveProjectStateChanged);
    }

    protected override void OnPaperStateChanged(Entity<CEResearchTableComponent> ent)
    {
        base.OnPaperStateChanged(ent);

        UpdateOpenUi(ent.Owner);
    }

    private void OnDraftStateChanged(Entity<CEUnselectedDiscoveryProjectComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateOpenUiForItem(ent.Owner);
    }

    // Also fires on every subsequent Tiles mutation, not just the initial spawn - this is what
    // keeps the hex grid live-updating as points get placed, without needing to reopen the window.
    private void OnActiveProjectStateChanged(Entity<CEDiscoveryProjectComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateOpenUiForItem(ent.Owner);
    }

    private void UpdateOpenUiForItem(EntityUid item)
    {
        var parent = Transform(item).ParentUid;
        if (HasComp<CEResearchTableComponent>(parent))
            UpdateOpenUi(parent);
    }

    private void UpdateOpenUi(EntityUid table)
    {
        if (_userInterface.TryGetOpenUi(table, CEResearchTableUiKey.Key, out var bui))
            bui.Update();
    }
}
