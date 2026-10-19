namespace Content.Shared._CE.MagicVision.Components;

/// <summary>
/// Status effect that grants magic vision to the entity it is applied to for as long as it lasts.
/// </summary>
[RegisterComponent]
public sealed partial class CEMagicVisionStatusEffectComponent : Component
{
    /// <summary>
    /// Whether the screen-distorting overlay is shown while this effect grants vision.
    /// </summary>
    [DataField]
    public bool ShowOverlay = true;
}
