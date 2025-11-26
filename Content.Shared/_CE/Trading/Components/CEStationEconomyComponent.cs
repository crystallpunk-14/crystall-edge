using Content.Shared._CE.Trading.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Components;

/// <summary>
/// The server calculates all prices for all product items, saves them in this component at the station,
/// and synchronizes the data with the client for the entire round.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEStationEconomyComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CETradingPositionPrototype>, int> Pricing = new();

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CETradingRequestPrototype>, int> RequestPricing = new();

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CETradingFactionPrototype>, HashSet<ProtoId<CETradingRequestPrototype>> > ActiveRequests = new();

    [DataField]
    public int MaxRequestCount = 5;
}
