using Content.Shared._CE.Animation.Item.Components;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.MeleeWeapon.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(CEDualWieldSystem))]
public sealed partial class CEDualWieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<CEDualWieldAnimationSet> AnimationSets = new();
}

/// <summary>
/// Defines a set of dual-wield animations for a specific off-hand weapon tag and use type.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class CEDualWieldAnimationSet
{
    /// <summary>
    /// The tag the off-hand weapon must have for this animation set to apply.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    /// <summary>
    /// Which input button triggers this dual-wield animation set.
    /// </summary>
    [DataField(required: true)]
    public CEUseType UseType;

    /// <summary>
    /// Combo chain of animations to play for this pairing.
    /// </summary>
    [DataField(required: true)]
    public List<CEAnimationEntry> Animations = new();
}
