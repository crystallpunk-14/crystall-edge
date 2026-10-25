using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Traits;

/// <summary>
/// A character trait picked in the lobby. Its name, description and icon come from <see cref="Skill"/>;
/// the trait itself only describes the lobby-side choice (cost, unlocks, conflicts) and what is granted on spawn.
/// </summary>
[Prototype("trait")]
public sealed partial class CETraitPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Skill granted when this trait rolls on spawn.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CESkillPrototype> Skill;

    /// <summary>
    /// Points of this trait. Positive for beneficial traits, negative for harmful ones -
    /// a character's picked traits must sum to exactly zero.
    /// </summary>
    [DataField]
    public int Cost;

    /// <summary>
    /// Item put into the character's hands when this trait rolls on spawn.
    /// </summary>
    [DataField]
    public EntProtoId? Gear;

    /// <summary>
    /// Requirements to unlock this trait in the lobby (e.g. an achievement).
    /// </summary>
    [DataField]
    public HashSet<JobRequirement>? Requirements;

    /// <summary>
    /// Traits that can't be picked together with this one. Symmetric - listing a conflict on one side is enough.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<CETraitPrototype>> Incompatible = new();
}
