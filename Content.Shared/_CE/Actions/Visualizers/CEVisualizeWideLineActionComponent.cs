using Robust.Shared.Utility;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// When placed on an action entity, the client will draw a straight-line trajectory
/// from the player towards the cursor while the action is being targeted.
/// The line has configurable width, drawn with mirrored border sprites (start, middle, end).
/// </summary>
[RegisterComponent]
public sealed partial class CEVisualizeWideLineActionComponent : Component
{
    /// <summary>
    /// Half-width of the trajectory strip in world units.
    /// </summary>
    [DataField]
    public float Width = 0.5f;

    /// <summary>
    /// RSI for the border sprite (start cap).
    /// </summary>
    [DataField]
    public ResPath BorderStartSprite = new("/Textures/_CE/Actions/overlay.rsi");

    /// <summary>
    /// State inside the start-cap RSI.
    /// </summary>
    [DataField]
    public string BorderStartState = "border_start";

    /// <summary>
    /// RSI for the border sprite (stretched middle).
    /// </summary>
    [DataField]
    public ResPath BorderMidSprite = new("/Textures/_CE/Actions/overlay.rsi");

    /// <summary>
    /// State inside the middle RSI.
    /// </summary>
    [DataField]
    public string BorderMidState = "border_center";

    /// <summary>
    /// RSI for the border sprite (end cap).
    /// </summary>
    [DataField]
    public ResPath BorderEndSprite = new("/Textures/_CE/Actions/overlay.rsi");

    /// <summary>
    /// State inside the end-cap RSI.
    /// </summary>
    [DataField]
    public string BorderEndState = "border_end";

    /// <summary>
    /// Opacity of the interior fill.
    /// </summary>
    [DataField]
    public float FillAlpha = 0.05f;

    /// <summary>
    /// When true, the line always extends to full cast range in the cursor direction,
    /// reflecting projectile behavior (fires toward cursor, travels up to max range).
    /// When false (default), the line ends at the cursor position (clamped to range).
    /// </summary>
    [DataField]
    public bool ProjectileMode;
}
