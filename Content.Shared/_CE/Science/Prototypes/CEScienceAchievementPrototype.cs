using Content.Shared._CE.EntityEffect;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Science.Prototypes;

[Prototype("scienceAchievement")]
public sealed partial class CEScienceAchievementPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<CEScienceAreaPrototype> Area;

    /// <summary>
    /// How far (in cells, Chebyshev distance) from the map's center this achievement should be
    /// procedurally placed. Higher difficulty means farther from the starting cell.
    /// </summary>
    [DataField(required: true)]
    public int Difficulty;

    [DataField(required: true)]
    public List<CEEntityEffect> Effects = new();

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Desc;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Icon drawn on the map in place of the area's fallback "unknown" icon, once this
    /// achievement's cell has been researched. If unset, the fallback icon is used instead.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon;
}
