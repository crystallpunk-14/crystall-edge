using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Workbench.Prototypes;

[Prototype("CERecipeCategory")]
public sealed class CEWorkbenchRecipeCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public int Priority = 0; // In descending order. More means it will be first.
}
