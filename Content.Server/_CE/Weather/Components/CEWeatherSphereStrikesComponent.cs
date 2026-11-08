using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.Weather.Components;

/// <summary>
/// Put on a weather status effect entity: periodically strikes a random open-sky tile within <see cref="Radius"/>
/// of the Lucson Sphere on the weather's map, raising <see cref="CEWeatherTileAffectedEvent"/> for it.
/// Strikes nothing on maps without a sphere. Handled by <see cref="CEWeatherSphereStrikesSystem"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEWeatherSphereStrikesComponent : Component
{
    [DataField]
    public float Radius = 20f;

    /// <summary>
    /// Random tiles tried per strike before it is skipped.
    /// </summary>
    [DataField]
    public int Attempts = 20;

    [DataField]
    public TimeSpan MinFrequency = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan MaxFrequency = TimeSpan.FromSeconds(40);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextStrikeTime;
}
