using Robust.Shared.GameStates;

namespace Content.Shared._CE.FlyerBattery;

/// <summary>
/// Limit flight by internal battery
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause,
 Access(typeof(CEZFlyerBatterySystem))]
public sealed partial class CEZFlyerBatteryComponent : Component
{
    [DataField]
    public float EnergyDraw = 2f;

    [DataField]
    public TimeSpan EnergyConsumeFrequency = TimeSpan.FromSeconds(1f);

    [DataField, AutoPausedField]
    public TimeSpan NextConsumeTime = TimeSpan.Zero;
}
