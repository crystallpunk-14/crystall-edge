using Robust.Shared.GameStates;

namespace Content.Shared._CE.Power.Components;

/// <summary>
/// Enable and disable ambient sound when powered and unpowered
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEPowerAmbientSoundComponent : Component
{
}
/// <summary>
/// Enable and disable point light when powered and unpowered
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEPowerPointLightComponent : Component
{
}
