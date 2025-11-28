namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Restricts the use of this action, by spending stamina.
/// </summary>
[RegisterComponent]
public sealed partial class CEActionStaminaCostComponent : Component
{
    [DataField]
    public float Stamina = 0f;
}
