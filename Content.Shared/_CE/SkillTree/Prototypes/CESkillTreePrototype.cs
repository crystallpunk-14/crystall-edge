using System.Numerics;
using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.SkillTree.Prototypes;

/// <summary>
/// A standalone skill tree: a set of independently-rooted node chains, each wrapping a
/// <see cref="CESkillPrototype"/>. The skill system itself has no notion of trees - cost,
/// UI layout and prerequisite structure only exist here.
/// </summary>
[Prototype("skillTree")]
public sealed partial class CESkillTreePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Desc;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public string Parallax = "Default";

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

    /// <summary>
    /// Icon representing the tree's points currency, shown next to its point balance instead of
    /// spelling the word out.
    /// </summary>
    [DataField]
    public SpriteSpecifier? PointsIcon;

    [DataField]
    public SoundSpecifier LearnSound = new SoundCollectionSpecifier("CELearnSkill");

    /// <summary>
    /// Root nodes of the tree. Each node recursively owns its <see cref="CESkillTreeNode.Children"/> -
    /// a child is only learnable once its single parent is learned. A tree can have multiple
    /// independent roots.
    /// </summary>
    [DataField]
    public List<CESkillTreeNode> Nodes = new();
}

/// <summary>
/// One node of a <see cref="CESkillTreePrototype"/>: a learnable <see cref="CESkillPrototype"/>
/// plus its tree-local cost, UI position and the child nodes it unlocks.
/// </summary>
[DataDefinition]
public sealed partial class CESkillTreeNode
{
    [DataField(required: true)]
    public ProtoId<CESkillPrototype> Skill = default!;

    [DataField]
    public int Cost = 1;

    /// <summary>
    /// Position of this node on the tree's UI graph.
    /// </summary>
    [DataField]
    public Vector2 Position = new(0, 0);

    [DataField]
    public List<CESkillTreeNode> Children = new();
}
