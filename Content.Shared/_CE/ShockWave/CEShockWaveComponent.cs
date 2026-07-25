using Robust.Shared.GameStates;

namespace Content.Shared._CE.ShockWave;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class CEShockWaveComponent : Component
{
    /// <summary>
    ///     The rate at which the wave fades, lower values means it's active for longer.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float FalloffPower = 40.0f;

    /// <summary>
    ///     How sharp the wave distortion is. Higher values make the wave more pronounced.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Sharpness = 10.0f;

    /// <summary>
    ///     Width of the wave.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Width = 0.8f;

    /// <summary>
    ///     How long (in seconds) the visual wave animation should last in the overlay.
    ///     Should match the entity's TimedDespawnComponent lifetime.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 1.0f;
}
