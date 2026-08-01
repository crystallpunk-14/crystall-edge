namespace Content.Client._CE.InfusionAltar;

/// <summary>
/// Drives the entity's <see cref="PointLightComponent"/> color from
/// <see cref="Content.Shared._CE.InfusionAltar.CEInfusionAltarDangerVisuals.Level"/> appearance data.
/// </summary>
[RegisterComponent]
public sealed partial class CEInfusionAltarDangerVisualsComponent : Component
{
    [DataField]
    public Color UnstableColor = Color.Orange;

    [DataField]
    public Color CriticalColor = Color.Red;

    /// <summary>
    /// The light's originally-configured color, captured on first appearance change and restored for
    /// <see cref="Content.Shared._CE.InfusionAltar.CEInfusionAltarDangerLevel.Calm"/>.
    /// </summary>
    public Color? BaseColor;
}
