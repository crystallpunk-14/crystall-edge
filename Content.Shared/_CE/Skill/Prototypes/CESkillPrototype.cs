using System.Numerics;
using Content.Shared._CE.Skill.Effects;
using Content.Shared._CE.Skill.Restrictions;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill.Prototypes;

/// <summary>
/// A skill that can be learned by the player. Skills are grouped into trees, and each skill has a cost to learn, prerequisites, and an effect.
/// </summary>
[Prototype("skill")]
public sealed partial class CESkillPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Skill Title. If you leave null, the name will try to generate from Effect.GetName()
    /// </summary>
    [DataField]
    public LocId? Name = null;

    /// <summary>
    /// Skill Description. If you leave null, the description will try to generate from Effect.GetDescription()
    /// </summary>
    [DataField]
    public LocId? Desc = null;

    /// <summary>
    /// The tree this skill belongs to. This is used to group skills together in the UI.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CESkillTreePrototype> Tree = default!;

    /// <summary>
    ///  The cost to learn this skill. This is used to determine how many skill points are needed to learn the skill.
    /// </summary>
    [DataField]
    public float LearnCost = 1f;

    /// <summary>
    ///  Skill UI position. This is used to determine where the skill will be displayed in the skill tree UI.
    /// </summary>
    [DataField(required: true)]
    public Vector2 SkillUiPosition = default!;

    /// <summary>
    ///  Icon for the skill. This is used to display the skill in the skill tree UI.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    ///  Skill effect. This is used to determine what happens when the player learns the skill. If you leave null, the skill will not have any effect.
    ///  But the presence of the skill itself can affect some systems that check for the presence of certain skills.
    /// </summary>
    [DataField]
    public List<CESkillEffect> Effects = new();

    /// <summary>
    /// Skill restriction. Limiters on learning. Any reason why a player cannot learn this skill.
    /// </summary>
    [DataField]
    public List<CESkillRestriction> Restrictions = new();
}
