using Content.Shared._CE.SkillTree.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.SkillTree.Components;

/// <summary>
/// Tracks which skill trees an entity can learn from and how many points it has to spend in each.
/// A tree's points are its own currency - there is no separate currency prototype, the points are
/// simply keyed by the tree that spends them.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
[Access(typeof(CESkillTreeSystem))]
public sealed partial class CESkillTreeLearningComponent : Component
{
    /// <summary>
    /// Skill trees this entity is allowed to learn nodes from.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CESkillTreePrototype>> AvailableTrees = new();

    /// <summary>
    /// Points available to spend per tree.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CESkillTreePrototype>, float> Points = new();
}
