using Robust.Shared.GameStates;

namespace Content.Shared._CE.MedicalVision;

/// <summary>
/// Shows health and stamina bars above the hovered creature and makes the health examine exact.
/// Works on worn clothing and on status effects, never put it on the player directly.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEMedicalVisionComponent : Component
{
}
