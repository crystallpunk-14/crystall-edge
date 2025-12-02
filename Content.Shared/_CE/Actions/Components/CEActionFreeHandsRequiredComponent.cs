namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Requires the user to have at least one free hand to use this spell
/// </summary>
[RegisterComponent]
public sealed partial class CEActionFreeHandsRequiredComponent : Component
{
    [DataField]
    public int FreeHandRequired = 1;
}
