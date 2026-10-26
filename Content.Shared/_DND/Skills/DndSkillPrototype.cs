using Content.Shared._DND.Abilities;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Skills;

/// <summary>
/// A skill a class can offer. The listed ability is the one a check with this skill uses.
/// </summary>
[Prototype("dndSkill")]
public sealed partial class DndSkillPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField(required: true)]
    public LocId Description { get; private set; }

    [DataField(required: true)]
    public ProtoId<DndAbilityPrototype> Ability { get; private set; }
}
