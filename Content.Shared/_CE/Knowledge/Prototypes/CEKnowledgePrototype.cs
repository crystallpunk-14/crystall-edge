using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Knowledge.Prototypes;

/// <summary>
/// Something a character can know - the shared unit behind recipes, achievements and other
/// </summary>
[Prototype("CEKnowledge")]
public sealed partial class CEKnowledgePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CEKnowledgePrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// Blank book cover entity spawned when this knowledge is written down with a pen. Usually set
    /// once per science area's abstract base prototype rather than repeated on every entry.
    /// </summary>
    [DataField]
    public EntProtoId Book = "CEBookEmpty";
}
