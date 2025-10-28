using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels;

/// <summary>
/// Marks the entity as falling through zLevels right now
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEFallingZComponent : Component
{
    [DataField]
    public int FallingDistance = 0;
}
