using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.Temperature;

/// <summary>
///
/// </summary>
[RegisterComponent, Access(typeof(CETemperatureSystem))]
public sealed partial class CEEntityHeaterComponent : Component
{
    [DataField]
    public float Power = 500;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextHeat = TimeSpan.Zero;

    [DataField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(0.5f);
}
