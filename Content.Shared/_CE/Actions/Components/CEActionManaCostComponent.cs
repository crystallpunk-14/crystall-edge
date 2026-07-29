namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Restricts the use of this action, by spending mana or user requirements.
/// </summary>
[RegisterComponent]
public sealed partial class CEActionManaCostComponent : Component
{
    [DataField(required: true)]
    public int ManaCost = 1;

    /// <summary>
    /// Can the cost of casting this magic effect be changed from clothing or other sources?
    /// </summary>
    [DataField]
    public bool CanModifyManacost = true;
}
