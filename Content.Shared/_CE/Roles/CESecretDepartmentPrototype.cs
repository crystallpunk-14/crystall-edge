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
    /// Default pool of shared objectives granted independently to every player holding a role
    /// in this faction, unless a GameRule's
    /// <see cref="Content.Server._CE.Roles.CESecretRoleObjectivesOverrideComponent"/> overrides
    /// it for that round.
    /// </summary>
    [DataField]
    public CEObjectivePool? ObjectivePool;
}
