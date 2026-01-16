namespace Content.Shared._CE.Power.Components;

/// <summary>
/// When destroyed, it releases all the energy from BatteryComponent around it in the form of radiation.
/// </summary>
[RegisterComponent]
public sealed partial class CEIrradiateOnDestroyComponent : Component
{
    /// <summary>
    /// How long will the radiation spike last?
    /// </summary>
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// How much of the initial energy will be transformed into radiation?
    /// </summary>
    [DataField]
    public float IrradiateCoefficient = 0.5f;
}
