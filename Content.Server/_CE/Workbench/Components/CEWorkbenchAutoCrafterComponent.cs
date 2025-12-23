using Content.Shared._CE.Workbench.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Workbench;

/// <summary>
///
/// </summary>
[RegisterComponent]
[Access(typeof(CEWorkbenchSystem))]
public sealed partial class CEWorkbenchAutoCrafterComponent : Component
{
    [DataField]
    public ProtoId<CEWorkbenchRecipePrototype>? SelectedRecipe;
}
