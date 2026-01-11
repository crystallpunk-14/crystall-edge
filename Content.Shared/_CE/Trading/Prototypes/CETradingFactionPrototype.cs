using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Prototypes;

[Prototype("tradingFaction")]
public sealed partial class CETradingFactionPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public Color Color = Color.White;
}
