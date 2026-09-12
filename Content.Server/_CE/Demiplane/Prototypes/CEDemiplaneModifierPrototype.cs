using Content.Server._CE.Demiplane.Modifiers;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Prototypes;

/// <summary>
/// A modifier a demiplane location can be decorated with — the answer to "what's inside", picked
/// randomly per teleport subject to a per-category budget. Selection isn't implemented yet; this is
/// just the data shape. Server-only for the same reason as <see cref="CEDemiplaneLocationPrototype"/>:
/// its effects spawn/modify real maps and grids.
/// </summary>
[Prototype("demiplaneModifier")]
public sealed partial class CEDemiplaneModifierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Difficulty range this modifier can be picked for.
    /// </summary>
    [DataField(required: true)]
    public MinMax Difficulty = new(0, 10);

    /// <summary>
    /// How often this modifier is picked relative to others competing for the same category budget.
    /// </summary>
    [DataField]
    public float GenerationWeight = 1f;

    /// <summary>
    /// Independent roll-to-skip even after being picked — lets a category sometimes generate nothing.
    /// </summary>
    [DataField]
    public float GenerationProb = 1f;

    /// <summary>
    /// Budget categories this modifier spends from, and how much of each.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CEDemiplaneModifierCategoryPrototype>, float> Categories = new();

    /// <summary>
    /// Can this modifier be picked more than once for the same location?
    /// </summary>
    [DataField]
    public bool Unique = true;

    /// <summary>
    /// The location must carry all of these tags for this modifier to be eligible.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> RequiredTags = new();

    /// <summary>
    /// What this modifier actually does when picked — each entry independently selected via
    /// <c>!type:</c>.
    /// </summary>
    [DataField]
    public List<ICEDemiplaneModifierEffect> Effects = new();
}
