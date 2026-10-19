using Content.Shared._CE.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Components;

/// <summary>
/// Marks an objective whose progress is the average survival of every player currently holding one
/// of <see cref="Roles"/> - see
/// <see cref="Content.Server._CE.Objectives.Systems.CEObjectiveRoleSurviveConditionSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEObjectiveRoleSurviveConditionComponent : Component
{
    [DataField(required: true)]
    public HashSet<ProtoId<CESecretRolePrototype>> Roles = new();

    /// <summary>
    /// Flips the objective into "every holder must be eliminated" - progress becomes the average
    /// of holders not surviving, and no holders at all counts as fully done.
    /// </summary>
    [DataField]
    public bool Invert;
}
