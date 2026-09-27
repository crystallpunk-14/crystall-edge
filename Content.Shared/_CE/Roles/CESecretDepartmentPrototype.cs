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
    /// Objectives shared by the whole faction
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
    /// examine text - including two holders of the exact same role, e.g. two unrelated Lovers. See
    /// <see cref="CESecretRoleIconComponent"/>.
    /// </summary>
    [DataField]
    public bool MembersRecognizeEachOther;
}
