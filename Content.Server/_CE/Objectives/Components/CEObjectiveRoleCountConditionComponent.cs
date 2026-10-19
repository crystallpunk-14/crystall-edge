using Content.Shared._CE.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Components;

/// <summary>
/// Marks an objective whose progress is how many players currently hold <see cref="Role"/>, out of
/// <see cref="Required"/> - which scales with the number of active players when the objective is
/// created. See <see cref="Content.Server._CE.Objectives.Systems.CEObjectiveRoleCountConditionSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEObjectiveRoleCountConditionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CESecretRolePrototype> Role;

    /// <summary>
    /// One more required role holder per this many active players.
    /// </summary>
    [DataField]
    public int PlayersPerHolder = 10;

    [DataField]
    public int MinRequired = 1;

    [DataField]
    public int MaxRequired = 3;

    /// <summary>
    /// Title LocId, passed the "count" argument once <see cref="Required"/> is known.
    /// </summary>
    [DataField]
    public LocId? Title;

    /// <summary>
    /// How many role holders are needed - fixed when the objective is created.
    /// </summary>
    [DataField]
    public int Required = 1;
}
