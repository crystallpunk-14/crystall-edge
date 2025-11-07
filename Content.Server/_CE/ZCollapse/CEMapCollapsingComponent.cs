namespace Content.Server._CE.ZCollapse;

/// <summary>
/// Allows tiles on this map to break when there are insufficient supports
/// </summary>
[RegisterComponent]
public sealed partial class CEMapCollapsingComponent : Component
{
    /// <summary>
    /// Stores the current structural support for each tile on MapGridComponent
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<Vector2i, int> CollapeTileDict = new();
}
