using Content.Shared._CE.Science;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Science;

public sealed class CEResearchTableBoundUserInterface : BoundUserInterface
{
    private CEResearchTableWindow? _window;

    public CEResearchTableBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CEResearchTableWindow>();
        _window.OnResearch += (area, coordinate, action) => SendMessage(new CEResearchTableActionMessage(area, coordinate, action));
        _window.OnChooseDiscovery += (area, coordinate, discovery) => SendMessage(new CEResearchTableChooseDiscoveryMessage(area, coordinate, discovery));
        _window.OnMergeEssence += (first, second) => SendMessage(new CEResearchTableMergeEssenceMessage(first, second));

        EntMan.System<CEClientScienceSystem>().OnLocalResearchDataUpdated += OnLocalResearchDataUpdated;
    }

    private void OnLocalResearchDataUpdated()
    {
        _window?.RefreshLocalData();
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

        if (state is CEResearchTableState researchState)
            _window?.UpdateState(researchState);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is CEResearchTableHypothesisResultMessage hypothesisResult)
            _window?.HandleHypothesisResult(hypothesisResult);
    }
}
