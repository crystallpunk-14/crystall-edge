using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill.Prototypes;

/// <summary>
/// A group of skills combined into one “branch”
/// </summary>
[Prototype("skillTree")]
public sealed partial class CESkillTreePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public ProtoId<CESkillPointPrototype> SkillType;

    [DataField]
    public SpriteSpecifier? FrameIcon;

    [DataField]
    public SpriteSpecifier? HoveredIcon;

    [DataField]
    public SpriteSpecifier? SelectedIcon;

    [DataField]
    public SpriteSpecifier? LearnedIcon;

    [DataField]
    public SpriteSpecifier? AvailableIcon;

    [DataField]
    public string Parallax = "AspidParallax";

    [DataField]
    public LocId? Desc;

    [DataField]
    public Color Color;

    [DataField]
    public SoundSpecifier LearnSound = new SoundCollectionSpecifier("CELearnSkill");
}
