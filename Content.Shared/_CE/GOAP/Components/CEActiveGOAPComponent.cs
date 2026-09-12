using Robust.Shared.GameStates;

namespace Content.Shared._CE.GOAP.Components;

/// <summary>
/// Marker component for active GOAP-controlled NPCs.
/// Added by CEGOAPSystem when the NPC should be actively updated.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEActiveGOAPComponent : Component
{
}
