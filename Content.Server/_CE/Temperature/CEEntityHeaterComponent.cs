using Robust.Shared.GameStates;

namespace Content.Server._CE.Temperature;

/// <summary>
///
/// </summary>
[RegisterComponent, Access(typeof(CETemperatureSystem))]
public sealed partial class CEEntityHeaterComponent : Component
{
    [DataField]
    public float Power = 500;
}
