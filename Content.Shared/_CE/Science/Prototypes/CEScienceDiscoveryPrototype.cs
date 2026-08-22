using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._CE.Science.Prototypes;

[Prototype("scienceDiscovery")]
public sealed partial class CEScienceDiscoveryPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CEScienceDiscoveryPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

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
    /// The skill this discovery teaches once chosen. Also supplies this discovery's display name,
    /// map icon and eligibility (<see cref="CESkillPrototype.Conditions"/>).
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CESkillPrototype> Skill;
}
