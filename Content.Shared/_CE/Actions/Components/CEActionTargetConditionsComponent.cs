using Content.Shared._CE.EntityEffect;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Blocks the action unless the target entity passes every one of <see cref="Conditions"/>.
/// The user is the condition's user, the targeted entity is its target.
/// </summary>
[RegisterComponent]
public sealed partial class CEActionTargetConditionsComponent : Component
{
    [DataField(required: true)]
    public List<CEEntityCondition> Conditions = new();
}
