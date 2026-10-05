namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Puts the action on a cooldown of random length after every use and when it is first granted,
/// replacing its fixed use delay.
/// </summary>
[RegisterComponent]
public sealed partial class CEActionRandomCooldownComponent : Component
{
    [DataField(required: true)]
    public TimeSpan Min;

    [DataField(required: true)]
    public TimeSpan Max;
}
