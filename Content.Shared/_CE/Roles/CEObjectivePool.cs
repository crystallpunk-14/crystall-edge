using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roles;

/// <summary>
/// A pool of objectives a secret role or secret department can hand out, drawn at random up to
/// a difficulty budget - mirrors upstream AntagRandomObjectivesComponent's Sets/MaxDifficulty.
/// </summary>
[DataDefinition]
public sealed partial class CEObjectivePool
{
    /// <summary>
    /// Each set of objectives to try picking.
    /// </summary>
    [DataField(required: true)]
    public List<CEObjectiveSet> Sets = new();

    /// <summary>
    /// If the total difficulty of the currently given objectives exceeds this, no more will be given.
    /// </summary>
    [DataField(required: true)]
    public float MaxDifficulty;
}

/// <summary>
/// A set of objectives to try picking. Difficulty is checked over all sets in a
/// <see cref="CEObjectivePool"/>, but each set has its own probability and pick count.
/// </summary>
[DataRecord]
public partial record struct CEObjectiveSet()
{
    /// <summary>
    /// The grouping used by the objective system to pick random objectives.
    /// First a group is picked from these, then an objective from that group.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<WeightedRandomPrototype> Groups = string.Empty;

    /// <summary>
    /// Probability of this set being used.
    /// </summary>
    [DataField]
    public float Prob = 1f;

    /// <summary>
    /// Number of times to try picking objectives from this set.
    /// Even if there is enough difficulty remaining, no more will be given after this.
    /// </summary>
    [DataField]
    public int MaxPicks = 20;
}
