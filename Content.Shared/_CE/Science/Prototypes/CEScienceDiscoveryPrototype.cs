using Content.Shared._CE.Knowledge.Prototypes;
using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Prototypes;

[Prototype("scienceDiscovery")]
public sealed partial class CEScienceDiscoveryPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<CEScienceAreaPrototype> Area;

    /// <summary>
    /// How many research points of each essence type choosing this discovery's card costs.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Cost = new();

    /// <summary>
    /// The knowledge this discovery teaches once chosen. Also supplies this discovery's
    /// display name and map icon.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CEKnowledgePrototype> Knowledge;
}
