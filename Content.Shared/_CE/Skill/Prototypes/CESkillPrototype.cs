using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Skill.Effects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill.Prototypes;

/// <summary>
/// Something a character can learn - the shared unit behind active perks, recipes, achievements
/// and other unlockable content. Learning one is a simple, idempotent set-membership operation
/// (<see cref="CESharedSkillSystem.TryAddSkill"/>); an optional <see cref="Effect"/> additionally
/// mutates the learner (grants an action, adds components, etc).
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
    /// Entity prototype this skill is themed after. By default its name, description and
    /// (multi-layer) sprite are used as-is instead of duplicating them into separate localization
    /// strings - see <see cref="NameOverride"/>/<see cref="DescOverride"/>/<see cref="IconOverride"/>
    /// to replace any of those individually.
    /// </summary>
    [DataField]
    public EntProtoId? PreviewEntity;

    /// <summary>
    /// Skill Title. If you leave null, the name will try to generate from PreviewEntity or Effect.GetName()
    /// </summary>
    [DataField("name")]
    public LocId? NameOverride = null;

    /// <summary>
    /// Skill Description. If you leave null, the description will try to generate from PreviewEntity or Effect.GetDescription()
    /// </summary>
    [DataField("desc")]
    public LocId? DescOverride = null;

    /// <summary>
    /// Icon for the skill. If you leave null, the icon will try to generate from Effect.GetIcon()
    /// </summary>
    [DataField("icon")]
    public SpriteSpecifier? IconOverride;

    /// <summary>
    /// Blank book cover entity spawned when this skill is written down with a pen. Usually set
    /// once per domain's abstract base prototype rather than repeated on every entry.
    /// </summary>
    [DataField]
    public EntProtoId Book = "CEBookEmpty";

    /// <summary>
    /// Skill effect. Used to determine what happens when the player learns the skill. Optional -
    /// a skill with no effect is a pure "known/not known" flag, gating whatever external systems
    /// (recipes, achievements, ...) choose to check it.
    /// </summary>
    [DataField]
    public CESkillEffect? Effect;

    /// <summary>
    /// Conditions an actor must pass to be allowed to learn this skill (e.g. already knowing a
    /// prerequisite skill). Checked by <see cref="CESharedSkillSystem.SkillConditionsMet"/> at
    /// every player-facing "try to learn" entry point. Empty means no restriction.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public List<CEEntityCondition> Conditions = new();

    /// <summary>
    /// Color used to highlight this skill's name in the learned-skills examine text.
    /// </summary>
    [DataField]
    public Color Color = Color.White;
}
