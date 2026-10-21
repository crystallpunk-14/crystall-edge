namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// allows the plant to receive energy passively, depending on the light level around it (sky and lamps)
/// </summary>
[RegisterComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantEnergyFromLightComponent : Component
{
    /// <summary>
    /// Energy per update at full light (or full darkness if <see cref="Inverted"/>), scaled by the light level
    /// </summary>
    [DataField]
    public float Energy;

    /// <summary>
    /// If true, the plant prefers darkness: it gains <c>Energy * (1 - level)</c> and nothing above <see cref="MaxLevel"/>.
    /// Otherwise it gains <c>Energy * level</c> and nothing below <see cref="MinLevel"/>.
    /// </summary>
    [DataField]
    public bool Inverted;

    /// <summary>
    /// Light level below which a light-loving plant gets no energy
    /// </summary>
    [DataField]
    public float MinLevel = 0.3f;

    /// <summary>
    /// Light level above which a shade-loving plant gets no energy
    /// </summary>
    [DataField]
    public float MaxLevel = 0.3f;
}
