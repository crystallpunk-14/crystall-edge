namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// When placed on an action entity, the client will highlight the tile currently under the cursor
/// while the action is being targeted.
/// The highlight snaps to the grid tile and never leaves the action range.
/// </summary>
[RegisterComponent]
public sealed partial class CEVisualizeTileActionComponent : Component
{
    /// <summary>
    /// Fill opacity for the tile highlight square.
    /// </summary>
    [DataField]
    public float FillAlpha = 0.25f;

    /// <summary>
    /// Border opacity for the tile highlight square.
    /// </summary>
    [DataField]
    public float BorderAlpha = 0.8f;
}
