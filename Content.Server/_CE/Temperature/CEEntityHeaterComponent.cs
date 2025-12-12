namespace Content.Server._CE.Temperature;

[RegisterComponent, Access(typeof(CETemperatureSystem)), AutoGenerateComponentPause]
public sealed partial class CEEntityHeaterComponent : Component
{
    [DataField]
    public float Power = 500;

    [DataField, AutoPausedField]
    public TimeSpan NextHeat = TimeSpan.Zero;

    [DataField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(0.5f);
}
