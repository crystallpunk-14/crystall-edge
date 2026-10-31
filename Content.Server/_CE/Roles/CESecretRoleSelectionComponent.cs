using Content.Shared._CE.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Roles;

/// <summary>
/// Placed on a GameRule entity. Lists every secret department that can appear this round, the share
/// of players each one gets and the roles it hands out. See <see cref="CESecretRoleSelectionSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(CESecretRoleSelectionSystem))]
public sealed partial class CESecretRoleSelectionComponent : Component
{
    [DataField(required: true)]
    public List<CESecretDepartmentSelectorEntry> Departments = new();

    /// <summary>
    /// How many of each role have been granted so far this round. Lets late joiners top up
    /// whatever's left without re-running the whole round-start draw.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CESecretRolePrototype>, int> AssignedCounts = new();

    /// <summary>
    /// Objective holder entity for each secret department that has drawn its pool this round. The
    /// holder owns the department's shared objectives directly; members don't own a copy - they
    /// pick them up dynamically (see <see cref="CESecretRoleSelectionSystem.OnGetAdditionalObjectives"/>)
    /// for as long as they hold a role in that department, the same way
    /// <c>ESSharedSecretIdentitySystem</c> does it for organizations upstream.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CESecretDepartmentPrototype>, EntityUid> DepartmentObjectiveHolders = new();
}

/// <summary>
/// A single entry in <see cref="CESecretRoleSelectionComponent.Departments"/>: how big a department
/// gets this round and which of its roles can be handed out.
/// </summary>
[DataDefinition]
public sealed partial class CESecretDepartmentSelectorEntry
{
    [DataField(required: true)]
    public ProtoId<CESecretDepartmentPrototype> Department;

    /// <summary>
    /// Fraction of the players this department aims to hold, e.g. 0.2 for 20%.
    /// </summary>
    [DataField(required: true)]
    public float Share;

    /// <summary>
    /// Below this many players the department gets no slots at all.
    /// </summary>
    [DataField]
    public int MinPlayers;

    [DataField(required: true)]
    public List<CESecretRoleSelectorEntry> Roles = new();

    public int GetTargetCount(int playerCount)
    {
        if (playerCount < MinPlayers)
            return 0;

        return (int) MathF.Round(playerCount * Share, MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// A single role inside a <see cref="CESecretDepartmentSelectorEntry"/>.
/// </summary>
[DataDefinition]
public sealed partial class CESecretRoleSelectorEntry
{
    [DataField(required: true)]
    public ProtoId<CESecretRolePrototype> Role;

    /// <summary>
    /// Relative frequency of this role among the department's other roles.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Hard cap on how many players can hold this role in a round. Null means no cap.
    /// </summary>
    [DataField]
    public int? Max;

    /// <summary>
    /// If true, the role is only handed out when no other role of the department is available to the
    /// player - unless the player set it to a priority above Low themselves.
    /// </summary>
    [DataField]
    public bool Fallback;
}
