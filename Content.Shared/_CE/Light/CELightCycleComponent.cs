using Robust.Shared.GameStates;

namespace Content.Shared._CE.Light;

/// <summary>
/// Add-on for <see cref="Content.Shared.Light.Components.LightCycleComponent"/> that overrides the vanilla
/// sine-wave color calculation with a cyclic gradient defined by a list of color stops.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CELightCycleComponent : Component
{
    /// <summary>
    /// Gradient stops keyed by position in the cycle, from 0 (cycle start) to 1 (cycle end, wraps back to key 0).
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<float, Color> Colors = new();
}
