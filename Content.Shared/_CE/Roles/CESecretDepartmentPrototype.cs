using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roles;

/// <summary>
/// A faction/grouping of <see cref="CESecretRolePrototype"/>s, displayed in the character
/// editor's secret roles section the same way <see cref="Content.Shared.Roles.DepartmentPrototype"/>
/// groups jobs.
/// </summary>
[Prototype("secretDepartment")]
public sealed partial class CESecretDepartmentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// The name LocId of the faction displayed in the character editor.
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// A description LocId explaining the faction's role, shown in the character editor.
    /// </summary>
    [DataField(required: true)]
    public LocId Description = string.Empty;

    /// <summary>
    /// A color representing this faction to use for text.
    /// </summary>
    [DataField(required: true)]
    public Color Color;

    [DataField]
    public List<ProtoId<CESecretRolePrototype>> Roles = new();

    /// <summary>
    /// Factions with a higher weight sort before other factions in the UI.
    /// </summary>
    [DataField]
    public int Weight;

    /// <summary>
    /// Objectives shared by the whole faction: drawn once per round and handed to every player
    /// holding a role in this faction (including late-joiners), rather than a personal copy per
    /// player. Overridable per round by a GameRule's
    /// <see cref="Content.Server._CE.Roles.CESecretRoleObjectivesOverrideComponent"/>. Mirrors ES's
    /// <c>ESOrganizationPrototype.Objectives</c>.
    /// </summary>
    [DataField]
    public EntityTableSelector? Objectives;

    /// <summary>
    /// Skills shared by the whole faction, granted directly to every player holding a role in it.
    /// </summary>
    [DataField]
    public List<ProtoId<CESkillPrototype>> Skills = new();

    /// <summary>
    /// Whether holders of any role in this department can see each other's secret role icon and
    /// examine text, even when they hold different roles within it. See
    /// <see cref="CESecretRoleIconComponent"/>. A role always recognizes its own kind regardless of
    /// this flag - this only controls cross-role recognition within the same department.
    /// </summary>
    [DataField]
    public bool MembersRecognizeEachOther;
}
