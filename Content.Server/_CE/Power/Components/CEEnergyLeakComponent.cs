namespace Content.Server._CE.Power.Components;

[RegisterComponent]
public sealed partial class CEEnergyLeakComponent : Component
{
    /// <summary>
    /// How much of the energy received is emitted as radiation?
    /// </summary>
    [DataField]
    public float LeakPercentage = 0.5f;
}
