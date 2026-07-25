using Robust.Shared.GameStates;

namespace Content.Shared._CE.MagicEssence.Components;

/// <summary>
/// Grants the wearer the thaumaturgy overlay, showing the essence composition of whatever
/// entity is currently under their cursor. Meant to be worn in the eyes slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEMagicEssenceScannerComponent : Component;
