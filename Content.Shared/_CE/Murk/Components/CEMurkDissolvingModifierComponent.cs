using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Multiplies how fast an entity dissolves and restores in the murk while this status effect is
/// active. Stacks multiplicatively across every active status effect with this component; 0
/// fully suppresses that direction.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEMurkDissolvingModifierComponent : Component
{
    /// <summary>
    /// Multiplies <see cref="CEMurkDissolvingComponent.DissolvingSpeed"/>.
    /// </summary>
    [DataField]
    public float DissolvingModifier = 1f;

    /// <summary>
    /// Multiplies <see cref="CEMurkDissolvingComponent.RestoringSpeed"/>.
    /// </summary>
    [DataField]
    public float RestoringModifier = 1f;
}
