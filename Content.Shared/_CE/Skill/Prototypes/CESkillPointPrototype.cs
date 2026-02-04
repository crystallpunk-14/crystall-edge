using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill.Prototypes;

/// <summary>
/// Defines a type of skill point currency used for learning skills.
/// </summary>
[Prototype("skillPoint")]
public sealed partial class CESkillPointPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public SpriteSpecifier? Icon;

    [DataField]
    public LocId? GetPointPopup;

    [DataField]
    public LocId? LosePointPopup;
}
