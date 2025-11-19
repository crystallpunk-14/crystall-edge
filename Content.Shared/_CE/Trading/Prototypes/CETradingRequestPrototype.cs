using Content.Shared._CE.Workbench;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Prototypes;

[Prototype("tradingRequest")]
public sealed partial class CETradingRequestPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField]
    public HashSet<ProtoId<CETradingFactionPrototype>> PossibleFactions = [];

    [DataField]
    public float GenerationWeight = 1f;

    [DataField]
    public int FromMinutes = 0;

    [DataField]
    public int? ToMinutes;

    [DataField]
    public int AdditionalReward = 10;

    [DataField(required: true)]
    public List<CEWorkbenchCraftRequirement> Requirements = new();
}
