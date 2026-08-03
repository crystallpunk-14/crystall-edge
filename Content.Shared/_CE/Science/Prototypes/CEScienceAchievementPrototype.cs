using Content.Shared._CE.Knowledge.Prototypes;
using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Prototypes;

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

    /// <summary>
    /// How many research points of each essence type completing this achievement's discovery
    /// costs, via the "discover achievement" research action.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Cost = new();

    /// <summary>
    /// The knowledge this achievement teaches once discovered. Also supplies this achievement's
    /// display name and map icon.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CEKnowledgePrototype> Knowledge;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(3);
}
