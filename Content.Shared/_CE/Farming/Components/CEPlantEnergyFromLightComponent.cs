namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// allows the plant to receive energy passively, depending on daylight
/// </summary>
[RegisterComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantEnergyFromLightComponent : Component
{
    [DataField]
    public float Energy;

    [DataField]
    public bool Daytime = true;

    [DataField]
    public bool Nighttime;
}
