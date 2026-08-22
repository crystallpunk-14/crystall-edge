using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Restricts the use of this action by requiring thaumaturgical essence of specific types and
/// amounts, drawn from the performer's active magic foci (see
/// <see cref="Content.Shared._CE.MagicFocus.Components.CEMagicFocusComponent"/>).
/// </summary>
[RegisterComponent]
public sealed partial class CEActionEssenceCostComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> EssenceCost = new();
}
