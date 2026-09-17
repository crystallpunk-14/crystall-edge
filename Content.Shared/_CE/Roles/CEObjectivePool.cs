using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roles;

/// <summary>
/// A pool of objectives a secret role or secret department can hand out, drawn at random up to
/// a difficulty budget.
/// </summary>
[DataDefinition]
public sealed partial class CEObjectivePool
{
    /// <summary>
    /// Candidate objectives and their pick weight.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, float> Weighted = new();

    /// <summary>
    /// Objectives keep getting picked from <see cref="Weighted"/> until their combined
    /// difficulty would exceed this.
    /// </summary>
    [DataField(required: true)]
    public float MaxDifficulty;
}
