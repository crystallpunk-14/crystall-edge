namespace Content.Server._CE.Power.Components;

/// <summary>
/// TODO
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEChargingPlatformComponent : Component
{
    [DataField]
    public bool Active = true;

    [DataField]
    public float Charge = 1;

    [DataField, AutoPausedField]
    public TimeSpan NextCharge = TimeSpan.Zero;

    [DataField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(0.5f);
}
