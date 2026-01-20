using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Prototypes;

/// <summary>
/// Localized food tag used by the cooking system.
/// </summary>
[Prototype("CEFoodTag")]
public sealed partial class CEFoodTagPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;
}
