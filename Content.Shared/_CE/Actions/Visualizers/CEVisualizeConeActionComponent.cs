using Robust.Shared.Utility;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// When placed on an action entity, the client will draw a cone in front of the player
/// while the action is being targeted.
/// The cone uses the same border sprites and translucent fill language as the other CE target visualizers.
/// </summary>
[RegisterComponent]
public sealed partial class CEVisualizeConeActionComponent : Component
{
    /// <summary>
    /// Angular width of the cone in degrees.
    /// </summary>
    [DataField]
    public float Width = 45f;

    /// <summary>
    /// Visual range of the cone in world units.
    /// If left at 0, the action's TargetAction range is used instead.
    /// </summary>
    [DataField]
    public float Range;

    /// <summary>
    /// RSI for the border sprite (start cap of each cone side).
    /// </summary>
    [DataField]
    public ResPath BorderStartSprite = new("/Textures/_CE/Actions/overlay.rsi");

    /// <summary>
    /// State inside the start-cap RSI.
    /// </summary>
    [DataField]
    public string BorderStartState = "border_start";

    /// <summary>
    /// RSI for the border sprite (stretched middle of each cone side).
    /// </summary>
    [DataField]
    public ResPath BorderMidSprite = new("/Textures/_CE/Actions/overlay.rsi");

    /// <summary>
    /// State inside the middle RSI.
    /// </summary>
    [DataField]
    public string BorderMidState = "border_center";

    /// <summary>
    /// RSI for the border sprite (end cap at the cone edge).
    /// </summary>
    [DataField]
    public ResPath BorderEndSprite = new("/Textures/_CE/Actions/overlay.rsi");

    /// <summary>
    /// State inside the end-cap RSI.
    /// </summary>
    [DataField]
    public string BorderEndState = "border_end";

    /// <summary>
    /// RSI path for the sprite placed along the outer cone arc.
    /// </summary>
    [DataField]
    public ResPath ArcSprite = new("/Textures/_CE/Actions/overlay.rsi");

    /// <summary>
    /// State inside the RSI used along the outer arc.
    /// </summary>
    [DataField]
    public string ArcState = "border_small";

    /// <summary>
    /// Visual size of each arc sprite in world units.
    /// </summary>
    [DataField]
    public float ArcSpriteSize = 0.5f;

    /// <summary>
    /// Distance between adjacent arc sprites in world units.
    /// Lower value = denser border.
    /// </summary>
    [DataField]
    public float ArcSpriteSpacing = 0.5f;

    /// <summary>
    /// Opacity of the cone fill.
    /// </summary>
    [DataField]
    public float FillAlpha = 0.05f;
}
