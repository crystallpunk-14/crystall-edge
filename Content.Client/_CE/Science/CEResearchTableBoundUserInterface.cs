using Content.Shared._CE.Science;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Science;

public sealed class CEResearchTableBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    //private CEResearchTableWindow? _window;

    protected override void Open()
    {
        base.Open();

        //_window = this.CreateWindow<CEResearchTableWindow>();

        EntMan.System<CEClientScienceSystem>().OnLocalResearchDataUpdated += OnLocalResearchDataUpdated;
    }

    private void OnLocalResearchDataUpdated()
    {
        //_window?.RefreshLocalData();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            EntMan.System<CEClientScienceSystem>().OnLocalResearchDataUpdated -= OnLocalResearchDataUpdated;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        //if (state is CEResearchTableState researchState)
        //    _window?.UpdateState(researchState);
    }
}
