using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Knowledge.Prototypes;

/// <summary>
/// Something a character can know - the shared unit behind recipes, achievements and other
/// "character knows X" systems. Carries the display identity (name/description/icon) that gets
/// written into a book and handed to another player via <see cref="Components.CEKnowledgeHolderComponent"/>.
/// </summary>
[Prototype("CEKnowledge")]
public sealed partial class CEKnowledgePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Description;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// Blank book entity spawned when this knowledge is written down with a pen. Multiple
    /// Knowledge entries can point at the same custom book to share a cover.
    /// </summary>
    [DataField]
    public EntProtoId Book = "CEBookEmpty";
}
