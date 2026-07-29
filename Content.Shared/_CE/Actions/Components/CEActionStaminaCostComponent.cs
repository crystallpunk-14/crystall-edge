namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Restricts the use of this action by spending stamina.
/// The action is blocked when the performer is exhausted or has zero stamina.
/// </summary>
[RegisterComponent]
public sealed partial class CEActionStaminaCostComponent : Component
{
    /// <summary>
    /// Amount of stamina consumed when the action is performed.
    /// </summary>
    [DataField(required: true)]
    public float Cost = 1f;
}
