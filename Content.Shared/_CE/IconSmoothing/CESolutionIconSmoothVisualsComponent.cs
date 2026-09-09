using Robust.Shared.GameStates;

namespace Content.Shared._CE.IconSmoothing;

/// <summary>
/// Authored IconSmooth state families for a solution's fill levels.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CESolutionIconSmoothVisualsComponent : Component
{
    /// <summary>
    /// State prefixes from empty to full, with at least one filled level.
    /// IconSmooth appends the topology suffix; no sprite naming convention is imposed here.
    /// </summary>
    [DataField(required: true)]
    public List<string> StateBases = new();
}
