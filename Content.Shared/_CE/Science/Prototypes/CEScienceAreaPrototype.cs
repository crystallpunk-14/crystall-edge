using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Science.Prototypes;

[Prototype("scienceArea")]
public sealed partial class CEScienceAreaPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId? Desc;

    [DataField]
    public Color Color = Color.White;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// Essence cost of starting a new research in this area.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Cost = new();

    /// <summary>
    /// Cover entity used for an active research project in this area, once a discovery is chosen.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Project = default!;
}
