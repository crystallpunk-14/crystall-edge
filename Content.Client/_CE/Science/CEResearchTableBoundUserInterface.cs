using Content.Shared._CE.Science;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Science;

public sealed class CEResearchTableBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CEResearchTableWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CEResearchTableWindow>();
        _window.SetEntity(Owner);

        EntMan.System<CEClientScienceSystem>().OnLocalResearchDataUpdated += OnLocalResearchDataUpdated;
    }

    public override void Update()
    {
        base.Update();

        _window?.UpdateUi();
    }

    private void OnLocalResearchDataUpdated()
    {
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            EntMan.System<CEClientScienceSystem>().OnLocalResearchDataUpdated -= OnLocalResearchDataUpdated;
    }

}
