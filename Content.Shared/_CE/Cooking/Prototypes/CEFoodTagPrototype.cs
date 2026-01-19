using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Prototypes;

[Prototype("CEFoodTag")]
public sealed partial class CEFoodTagPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;
}
