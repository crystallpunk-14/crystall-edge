using Content.Shared._CE.EntityEffect;
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
    /// The fixed aspect nodes placed around the research puzzle's edge that must all end up linked
    /// together through a chain of placed aspects for this discovery to be completed.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CEMagicEssenceTypePrototype>> TargetAspects = new();

    /// <summary>
    /// Parameters controlling the random puzzle map generated for this discovery's project.
    /// </summary>
    [DataField]
    public CEScienceMapGenerationParams Generation = new();

    /// <summary>
    /// The knowledge this discovery teaches once chosen. Also supplies this discovery's
    /// display name and map icon.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CEKnowledgePrototype> Knowledge;

    /// <summary>
    /// Conditions an actor must pass for this discovery to be eligible to be drawn into an offer
    /// (e.g. already knowing some prerequisite knowledge). Empty means no restriction.
    /// </summary>
    [DataField]
    public List<CEEntityCondition> Requirements = new();
}
