using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roadmap;

[Prototype("roadmapItem")]
public sealed partial class CERoadmapItemPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public RoadmapStatus Status;

    [DataField(required: true)]
    public ProtoId<CERoadmapItemCategory> Category;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Desc;
}

[Prototype("roadmapCategory")]
public sealed partial class CERoadmapItemCategory : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Title;

    [DataField(required: true)]
    public Color Color;

    [DataField]
    public int Priority = 0;
}

public enum RoadmapStatus
{
    Done,
    InProgress,
    Backlog
}
