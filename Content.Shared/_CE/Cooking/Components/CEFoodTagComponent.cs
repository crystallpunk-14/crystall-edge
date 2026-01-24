using Content.Shared._CE.Cooking.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Components;

/// <summary>
/// Holds food tags that identify this ingredient for cooking recipes.
/// Uses CEFoodTagPrototype instead of TagPrototype for localized display.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEFoodTagComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<CEFoodTagPrototype>> Tags = new();
}
