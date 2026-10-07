using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Components;

/// <summary>
/// Marks an objective that is done once no entity of any of <see cref="Prototypes"/> is left on the
/// station's z-network - progress is how many of those present at creation are gone. See
/// <see cref="Content.Server._CE.Objectives.Systems.CEObjectiveNoPrototypesConditionSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEObjectiveNoPrototypesConditionComponent : Component
{
    [DataField(required: true)]
    public HashSet<EntProtoId> Prototypes = new();

    /// <summary>
    /// How many matching entities existed when the objective was created.
    /// </summary>
    [DataField]
    public int InitialCount;
}
