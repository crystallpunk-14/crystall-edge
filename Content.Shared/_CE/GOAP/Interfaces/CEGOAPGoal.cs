namespace Content.Shared._CE.GOAP;

/// <summary>
/// A GOAP goal defining a desired world state with priority and activation conditions.
/// </summary>
[DataDefinition]
public sealed partial class CEGOAPGoal
{
    /// <summary>
    /// The desired world state that constitutes achieving this goal.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, bool> DesiredState = new();

    /// <summary>
    /// Conditions in the current world state that must match for this goal to be active.
    /// If empty, the goal is always considered.
    /// </summary>
    [DataField]
    public Dictionary<string, bool> Preconditions = new();

    /// <summary>
    /// Higher priority goals are preferred when multiple goals are active.
    /// </summary>
    [DataField]
    public float Priority = 1f;
}
