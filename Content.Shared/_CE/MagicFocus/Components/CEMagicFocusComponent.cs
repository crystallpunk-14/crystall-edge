using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicFocus.Components;

/// <summary>
/// A reservoir of thaumaturgical essence that spells can draw from.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEMagicFocusComponent : Component
{
    /// <summary>
    /// Max essence per type. Types not listed here use <see cref="MinimumVolume"/>.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Volume = new();

    /// <summary>
    /// Default max for types not listed in <see cref="Volume"/>.
    /// </summary>
    [DataField]
    public int MinimumVolume;

    /// <summary>
    /// Essence currently stored, per type. Missing type = 0.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> CurrentCharge = new();
}
