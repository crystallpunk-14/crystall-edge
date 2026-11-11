using Robust.Shared.GameStates;

namespace Content.Shared._CE.ManaVision;

/// <summary>
/// Shows a mana bar above the hovered creature or item that stores mana.
/// Works on worn clothing and on status effects, never put it on the player directly.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEManaVisionComponent : Component
{
}
