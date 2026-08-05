using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Robust.Client.GameObjects;

namespace Content.Client._CE.Science;

public sealed partial class CEResearchTableSystem : CESharedResearchTableSystem
{
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    protected override void OnPaperStateChanged(Entity<CEResearchTableComponent> ent)
    {
        base.OnPaperStateChanged(ent);

        if (_userInterface.TryGetOpenUi(ent.Owner, CEResearchTableUiKey.Key, out var bui))
            bui.Update();
    }
}
