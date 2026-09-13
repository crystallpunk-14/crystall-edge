using Content.Shared._CE.Roles;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Roles;

/// <summary>
/// Placed on a GameRule entity. Lists every secret role (of any faction) that can be granted
/// this round, along with how many of each to hand out. See <see cref="CESecretRoleSelectionSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(CESecretRoleSelectionSystem))]
public sealed partial class CESecretRoleSelectionComponent : Component
{
    [DataField(required: true)]
    public List<CESecretRoleSelectorEntry> Roles = new();

    /// <summary>
    /// How many of each role have been granted so far this round. Lets late joiners top up
    /// whatever's left without re-running the whole round-start draw.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CESecretRolePrototype>, int> AssignedCounts = new();
}

/// <summary>
/// A single entry in <see cref="CESecretRoleSelectionComponent.Roles"/>, describing how many
/// instances of a secret role to grant this round.
/// </summary>
[DataDefinition]
public sealed partial class CESecretRoleSelectorEntry
{
    [DataField(required: true)]
    public ProtoId<CESecretRolePrototype> Role;

    /// <summary>
    /// How many players are needed to "earn" one instance of this role, if scaled by population.
    /// A fixed count is just a degenerate range, e.g. <c>range: {min: 1, max: 1}</c>.
    /// </summary>
    [DataField]
    public int PlayerRatio = 10;

    /// <summary>
    /// Clamp for the population-scaled count. Ignored if <see cref="FillRemaining"/> is set.
    /// </summary>
    [DataField]
    public MinMax Range = new(0, int.MaxValue);

    /// <summary>
    /// If true, this role ignores <see cref="PlayerRatio"/>/<see cref="Range"/> entirely and
    /// absorbs every player left over after higher-weight roles have taken their share -
    /// a guaranteed fallback (e.g. Civilian), rather than a population-scaled slot count.
    /// </summary>
    [DataField]
    public bool FillRemaining;

    /// <summary>
    /// Roles with a higher weight have their candidate pool fully resolved (all priority tiers)
    /// before lower-weight roles are considered.
    /// </summary>
    [DataField]
    public int Weight;

    public int GetTargetCount(int playerCount)
    {
        if (FillRemaining)
            return int.MaxValue;

        return Math.Clamp(playerCount / PlayerRatio, (int) Range.Min, (int) Range.Max);
    }
}
