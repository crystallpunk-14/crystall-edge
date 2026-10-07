using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.MagicTrace.Components;

/// <summary>
/// An invisible trace left behind by magic use (or by a creature losing consciousness or dying), visible
/// only with magic vision. Its lifetime and fading are driven by
/// <see cref="Content.Shared._CE.TimedDespawn.CERandomizedTimedDespawnComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEMagicTraceComponent : Component
{
    /// <summary>
    /// Icon drawn on the trace, e.g. the icon of the spell that left it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Icon;

    /// <summary>
    /// Aura imprint of the creature that left this trace. Gets more obscured on examine as the trace fades.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? AuraImprint;

    [DataField, AutoNetworkedField]
    public Color AuraColor = Color.White;
}

[Serializable, NetSerializable]
public enum CEMagicTraceVisuals : byte
{
    Base,
    Icon,
}
