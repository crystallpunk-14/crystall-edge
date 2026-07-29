using Robust.Shared.Utility;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// When placed on an action entity, the client will draw an AoE zone circle
/// at the cursor (WorldTarget) or on a highlighted entity (EntityTarget)
/// while the action is being targeted.
/// </summary>
[RegisterComponent]
public sealed partial class CEVisualizeAoEZoneActionComponent : Component
{
    /// <summary>
    /// Radius of the AoE zone in world units.
    /// </summary>
    [DataField(required: true)]
    public float Radius = 1f;

    /// <summary>
    /// RSI path for the ring sprite placed along the zone circumference.
    /// </summary>
    [DataField]
    public ResPath Sprite = new("/Textures/_CE/Actions/overlay.rsi");

    /// <summary>
    /// State inside the RSI.
    /// </summary>
    [DataField]
    public string State = "border_small";

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
    public float SpriteSpacing = 0.5f;

    /// <summary>
    /// Opacity of the filled interior (0–1).
    /// </summary>
    [DataField]
    public float FillAlpha = 0.05f;

    /// <summary>
    /// When true, the AoE zone is drawn at the end of the cast range in the cursor direction,
    /// reflecting projectile behavior (projectile flies toward cursor, explodes at max range).
    /// When false (default), the zone follows the cursor position.
    /// </summary>
    [DataField]
    public bool ProjectileMode;
}
