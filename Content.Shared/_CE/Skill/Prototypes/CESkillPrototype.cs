using Content.Shared._CE.Skill.Effects;
using Content.Shared._CE.Skill.Restrictions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill.Prototypes;

/// <summary>
/// A skill that can be learned by the player. Skills have prerequisites and an effect.
/// </summary>
[Prototype("skill")]
public sealed partial class CESkillPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CESkillPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Skill Title. If you leave null, the name will try to generate from Effect.GetName()
    /// </summary>
    [DataField("name")]
    public LocId? NameOverride = null;

    /// <summary>
    /// Skill Description. If you leave null, the description will try to generate from Effect.GetDescription()
    /// </summary>
    [DataField("desc")]
    public LocId? DescOverride = null;

    /// <summary>
    /// Icon for the skill. If you leave null, the icon will try to generate from Effect.GetIcon()
    /// </summary>
    [DataField("icon")]
    public SpriteSpecifier? IconOverride;

    /// <summary>
    /// Skill effect. This is used to determine what happens when the player learns the skill.
    /// </summary>
    [DataField(required: true)]
    public CESkillEffect Effect = default!;

    /// <summary>
    /// Skill restriction. Any reason why a player cannot learn this skill.
    /// </summary>
    [DataField(serverOnly: true)]
    [AlwaysPushInheritance]
    public List<CESkillRestriction> Restrictions = new();

    /// <summary>
    /// The visual effect visible around the skill while it is in the world as a pickable enhancement.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Vfx;

    /// <summary>
    /// Light color for the skill while it is in the world as a pickable enhancement.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Whether this skill can only be learned once.
    /// </summary>
    [DataField]
    public bool Unique = true;

    /// <summary>
    /// Relative weight of this skill when it is randomly selected for a player.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public float Weight = 1f;
}
