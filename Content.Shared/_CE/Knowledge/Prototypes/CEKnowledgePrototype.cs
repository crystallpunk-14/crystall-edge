using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Knowledge.Prototypes;

/// <summary>
/// Something a character can know - the shared unit behind recipes, achievements and other
/// </summary>
[Prototype("CEKnowledge")]
public sealed partial class CEKnowledgePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// Blank book cover entity spawned when this knowledge is written down with a pen.
    /// </summary>
    [DataField]
    public EntProtoId Book = "CEBookEmpty";
}
