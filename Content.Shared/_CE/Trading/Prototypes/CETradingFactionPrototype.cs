using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Prototypes;

[Prototype("tradingFaction")]
public sealed partial class CETradingFactionPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = default!;

    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// faction is automatically unlocked for players at roundstart
    /// </summary>
    [DataField]
    public bool RoundStart = false;
}
