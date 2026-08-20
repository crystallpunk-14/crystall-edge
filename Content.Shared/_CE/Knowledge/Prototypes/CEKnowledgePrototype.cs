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

    /// <summary>
    /// Entity prototype this knowledge is themed after. By default its name, description and
    /// (multi-layer) sprite are used as-is instead of duplicating them into separate localization
    /// strings - see <see cref="NameOverride"/>/<see cref="DescOverride"/>/<see cref="IconOverride"/>
    /// to replace any of those individually.
    /// </summary>
    [DataField]
    public EntProtoId? PreviewEntity;

    [DataField]
    public LocId? NameOverride;

    [DataField]
    public LocId? DescOverride;

    /// <summary>
    /// Flat icon texture, for knowledge with no fitting <see cref="PreviewEntity"/>. Takes priority
    /// over <see cref="PreviewEntity"/>'s own sprite when both are set.
    /// </summary>
    [DataField]
    public SpriteSpecifier? IconOverride;

    /// <summary>
    /// Blank book cover entity spawned when this knowledge is written down with a pen. Usually set
    /// once per science area's abstract base prototype rather than repeated on every entry.
    /// </summary>
    [DataField]
    public EntProtoId Book = "CEBookEmpty";

    public string GetTitle(IPrototypeManager protoManager)
    {
        if (NameOverride is { } name)
            return Loc.GetString(name);

        return GetPreviewEntityProto(protoManager)?.Name ?? ID;
    }

    public string GetDescription(IPrototypeManager protoManager)
    {
        if (DescOverride is { } desc)
            return Loc.GetString(desc);

        return GetPreviewEntityProto(protoManager)?.Description ?? string.Empty;
    }

    /// <summary>
    /// Flat icon texture to show, if any - <see cref="IconOverride"/> only. Knowledge previewed via
    /// <see cref="PreviewEntity"/> should be rendered through <see cref="GetPreviewEntity"/> instead,
    /// so multi-layer/colored sprites still work.
    /// </summary>
    public SpriteSpecifier? GetIconTexture()
    {
        return IconOverride;
    }

    /// <summary>
    /// Entity prototype to render a preview of, or null if <see cref="IconOverride"/> should be used
    /// instead (see <see cref="GetIconTexture"/>).
    /// </summary>
    public EntityPrototype? GetPreviewEntity(IPrototypeManager protoManager)
    {
        return IconOverride is null ? GetPreviewEntityProto(protoManager) : null;
    }

    private EntityPrototype? GetPreviewEntityProto(IPrototypeManager protoManager)
    {
        return PreviewEntity is { } previewId && protoManager.TryIndex(previewId, out var indexedProto)
            ? indexedProto
            : null;
    }
}
