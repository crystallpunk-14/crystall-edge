using Robust.Shared.GameStates;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// A simple marker component for a <see cref="StatusEffectComponent"/> which allows this status effect to be cloned
/// by the CloningSystem (for example for paradox clones or cloning pods).
/// This is used for traits that use permanent status effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CloneableStatusEffectComponent : Component;
