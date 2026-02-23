using Content.Server._White.Temperature.Systems;

namespace Content.Server._White.Temperature.Components;

/// <summary>
/// passively returns the solution temperature to the standard
/// </summary>
[RegisterComponent, Access(typeof(WhiteSolutionTemperatureSystem))]
public sealed partial class WhiteSolutionTemperatureComponent : Component
{
    [DataField]
    public float StandardTemp = 300f;
}
