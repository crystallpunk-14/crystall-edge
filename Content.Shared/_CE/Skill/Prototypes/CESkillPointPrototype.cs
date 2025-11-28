using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill.Prototypes;

/// <summary>
/// A group of skills combined into one “branch”
/// </summary>
[Prototype("skillPoint")]
public sealed partial class CESkillPointPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public SpriteSpecifier? Icon;

    [DataField]
    public LocId? GetPointPopup;

    [DataField]
    public LocId? LosePointPopup;
}
