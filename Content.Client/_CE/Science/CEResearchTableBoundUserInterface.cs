using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Prototypes;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Science;

public sealed class CEResearchTableBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CEResearchTableWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CEResearchTableWindow>();
        _window.SetEntity(Owner);
        _window.OnMergeAspects += OnMergeAspects;
        _window.OnStartResearch += OnStartResearch;
        _window.OnChooseDiscovery += OnChooseDiscovery;
        _window.OnPlaceAspect += OnPlaceAspect;

        EntMan.System<CEClientScienceSystem>().OnLocalResearchDataUpdated += OnLocalResearchDataUpdated;
    }

    private void OnMergeAspects(ProtoId<CEMagicEssenceTypePrototype> first, ProtoId<CEMagicEssenceTypePrototype> second)
    {
        SendMessage(new CEResearchTableMergeAspectsMessage(first, second));
    }

    private void OnStartResearch(ProtoId<CEScienceAreaPrototype> area)
    {
        SendMessage(new CEResearchTableStartResearchMessage(area));
    }

    private void OnChooseDiscovery(ProtoId<CEScienceDiscoveryPrototype> discovery)
    {
        SendMessage(new CEResearchTableChooseDiscoveryMessage(discovery));
    }

    private void OnPlaceAspect(Vector2i hex, ProtoId<CEMagicEssenceTypePrototype> essence)
    {
        SendMessage(new CEResearchTablePlaceAspectMessage(hex, essence));
    }

    public override void Update()
    {
        base.Update();

        _window?.UpdateUi();
    }

    private void OnLocalResearchDataUpdated()
    {
        _window?.UpdateUi();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            EntMan.System<CEClientScienceSystem>().OnLocalResearchDataUpdated -= OnLocalResearchDataUpdated;

            if (_window is { } window)
            {
                window.OnMergeAspects -= OnMergeAspects;
                window.OnStartResearch -= OnStartResearch;
                window.OnChooseDiscovery -= OnChooseDiscovery;
                window.OnPlaceAspect -= OnPlaceAspect;
            }
        }
    }

}
