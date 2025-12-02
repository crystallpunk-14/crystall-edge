using Content.Shared._CE.Workbench;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Requires the caster to hold a specific resource in their hand, which will be spent to use the spell.
/// </summary>
[RegisterComponent]
public sealed partial class CEActionMaterialCostComponent : Component
{
    [DataField(required: true)]
    public CEWorkbenchCraftRequirement Requirement;
}
