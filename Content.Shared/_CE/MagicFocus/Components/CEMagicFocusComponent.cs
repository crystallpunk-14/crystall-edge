using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Audio;
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

    /// <summary>
    /// How long it takes to charge this focus from a clicked liquid container.
    /// </summary>
    [DataField]
    public TimeSpan ChargeDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Max number of distinct essence types (with a <see cref="CurrentCharge"/> above 0) this focus
    /// can hold at once. Charging can still top up a type that's already present past this limit,
    /// but can never introduce a new type once it's reached.
    /// </summary>
    [DataField]
    public int MaxEssenceTypes = 5;

    /// <summary>
    /// VFX entity spawned client-side (one per essence type absorbed, tinted to that type's color
    /// with a random rotation) when charging completes.
    /// </summary>
    [DataField]
    public EntProtoId ChargeEffect = "CEEssenceConsumeVFX";

    /// <summary>
    /// Sound played when charging completes.
    /// </summary>
    [DataField]
    public SoundSpecifier? ChargeSound = new SoundPathSpecifier("/Audio/_CE/Effects/essence_consume.ogg");
}
