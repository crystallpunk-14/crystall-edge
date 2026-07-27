using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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

    /// <summary>
    /// Guidebook flavour text for this aspect.
    /// </summary>
    [DataField]
    public LocId? Desc;

    [DataField(required: true)]
    public Color Color = Color.White;

    /// <summary>
    /// Thaumaturgical tier of this aspect, for guidebook grouping and sorting. 0 = primal aspects
    /// that aren't synthesized from anything else.
    /// </summary>
    [DataField]
    public int Tier;

    [DataField]
    public EntProtoId? EssenceProto;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// The liquid reagent that is the pure embodiment of this essence type. 1 unit of this reagent
    /// is worth 1 unit of this essence, used by <see cref="Systems.CEMagicEssenceSystem"/> to price
    /// solutions holding it.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>? Reagent;
}
