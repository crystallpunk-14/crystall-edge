using Robust.Shared.Utility;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// When placed on an action entity, the client will draw a cast-radius circle
/// around the player while the action is being targeted.
/// Sprites are placed along the circumference, facing inward.
/// </summary>
[RegisterComponent]
public sealed partial class CEVisualizeRadiusTargetActionComponent : Component
{
    /// <summary>
    /// RSI path for the ring sprite placed along the circumference.
    /// </summary>
    [DataField]
    public ResPath Sprite = new("/Textures/_CE/Actions/overlay.rsi");

    /// <summary>
    /// State inside the RSI.
    /// </summary>
    [DataField]
    public string State = "border";

    /// <summary>
    /// Visual size of each ring sprite in world units.
    /// </summary>
    [DataField]
    public float SpriteSize = 0.5f;

    /// <summary>
    /// Distance between the centres of adjacent ring sprites along the circumference (world units).
    /// Lower value = more sprites, higher density.
    /// </summary>
    [DataField]
    public float SpriteSpacing = 1f;

    /// <summary>
    /// Opacity of the filled interior (0–1).
    /// </summary>
    [DataField]
    public float FillAlpha = 0.05f;
}
