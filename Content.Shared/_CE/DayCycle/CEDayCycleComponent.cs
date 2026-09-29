using Robust.Shared.GameStates;

namespace Content.Shared._CE.DayCycle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEDayCycleComponent : Component
{
    public float LastLightLevel = 0f;

    /// <summary>
    /// Light level at or above which it is considered day on the map
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Threshold = 0.6f;
}
