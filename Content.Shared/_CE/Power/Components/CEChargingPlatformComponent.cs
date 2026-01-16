namespace Content.Shared._CE.Power.Components;

/// <summary>
/// Charging platform component for items with batteries.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEChargingPlatformComponent : Component
{
    /// <summary>
    /// Logical enable flag for the platform. When <c>false</c>, the system may skip charging.
    /// </summary>
    [DataField]
    public bool Active = true;

    /// <summary>
    /// Total charge added per charging tick across the platform.
    /// If multiple items are present, the charge is split evenly among them.
    /// </summary>
    [DataField]
    public float Charge = 1;

    /// <summary>
    /// Timestamp of the next scheduled charging. Updated by the system each cycle
    /// with <c>NextCharge = CurTime + Frequency</c>.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextCharge = TimeSpan.Zero;

    /// <summary>
    /// Charging cadence for the platform.
    /// </summary>
    [DataField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(0.5f);
}
