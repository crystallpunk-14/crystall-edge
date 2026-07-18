using Content.Shared._CE.EntityEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Prototypes;

[Prototype("scienceAchievement")]
public sealed partial class CEScienceAchievementPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<CEScienceAreaPrototype> Area;

    [DataField(required: true)]
    public List<CEEntityEffect> Effects = new();

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Desc;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(3);
}
