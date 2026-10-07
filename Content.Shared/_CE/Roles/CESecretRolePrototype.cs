using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._CE.Roles;

/// <summary>
/// Describes a single secret (hidden) role — not a station job, not assigned through
/// the normal job-slot pipeline, but shown in the character editor the same way a job is:
/// grouped under a <see cref="CESecretDepartmentPrototype"/>, with an icon, a priority
/// selector and a loadout button.
/// </summary>
[Prototype("secretRole")]
public sealed partial class CESecretRolePrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CESecretRolePrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// The name of this role as displayed to players.
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    /// <summary>
    /// The description of this role as displayed to players.
    /// </summary>
    [DataField]
    public string? Description;

    [ViewVariables(VVAccess.ReadOnly)]
    public string? LocalizedDescription => Description is null ? null : Loc.GetString(Description);

    [DataField]
    public ProtoId<JobIconPrototype> Icon = "JobIconUnknown";

    /// <summary>
    /// Should this role appear in the character editor? Roles that are only ever granted mid-round
    /// (e.g. by recruitment) still belong to their department, they just can't be picked.
    /// </summary>
    [DataField]
    public bool SetPreference = true;

    /// <summary>
    /// Requirements to select this role in the character editor.
    /// </summary>
    [DataField]
    public HashSet<JobRequirement>? Requirements;

    /// <summary>
    /// Personal objectives granted to a player holding this role, unless a GameRule's
    /// <see cref="Content.Server._CE.Roles.CESecretRoleObjectivesOverrideComponent"/> overrides it
    /// for that round. Mirrors ES's <c>ESSecretIdentityPrototype.Objectives</c>.
    /// </summary>
    [DataField]
    public EntityTableSelector? Objectives;

    /// <summary>
    /// Skills granted directly to a player holding this role.
    /// </summary>
    [DataField]
    public List<ProtoId<CESkillPrototype>> Skills = new();

    /// <summary>
    /// Gear handed to a player when they receive this role - at round start and on a mid-round
    /// role change alike. Never taken back when the role is removed.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? Gear;

    /// <summary>
    /// Players with any of these jobs are ineligible for this role.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>> ProhibitedJobs = new();
}
