using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicEssence.Prototypes;

/// <summary>
/// An aspect of thaumaturgical essence (e.g. Earth, Fire, Order, Chaos).
/// Essences of a given type can be combined with others to derive higher-tier aspects.
/// </summary>
[Prototype("magicEssenceType")]
public sealed partial class CEMagicEssenceTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public Color Color = Color.White;

    [DataField]
    public EntProtoId? EssenceProto;
}
