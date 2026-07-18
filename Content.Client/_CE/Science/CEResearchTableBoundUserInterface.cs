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
        _window.OnResearch += (area, coordinate) => SendMessage(new CEResearchTableResearchMessage(area, coordinate));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CEResearchTableState researchState)
            _window?.UpdateState(researchState);
    }
}
