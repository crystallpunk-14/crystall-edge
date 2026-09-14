using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared._CE.SkillTree.Components;
using Content.Shared._CE.SkillTree.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.SkillTree;

public sealed partial class CESkillTreeSystem : EntitySystem
{
    private static readonly HashSet<ProtoId<CESkillTreePrototype>> EmptyTrees = new();

    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private CESharedSkillSystem _skill = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <summary>
    /// Fired on the client whenever a watched entity's <see cref="CESkillTreeLearningComponent"/>
    /// state is applied - e.g. after a network update.
    /// </summary>
    public event Action<EntityUid>? OnSkillTreeUpdate;

    [SubscribeLocalEvent]
    private void OnAfterAutoHandleState(Entity<CESkillTreeLearningComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OnSkillTreeUpdate?.Invoke(ent.Owner);
    }

    /// <summary>
    /// Skill trees the entity currently has access to. [Access] only grants outside callers plain
    /// field reads, not method calls (Contains/TryGetValue/foreach) on the component's collections -
    /// this is the sanctioned way to actually query them.
    /// </summary>
    public IReadOnlySet<ProtoId<CESkillTreePrototype>> GetAvailableTrees(EntityUid target,
        CESkillTreeLearningComponent? component = null)
    {
        return Resolve(target, ref component, false) ? component.AvailableTrees : EmptyTrees;
    }

    /// <summary>
    /// The entity's current point balance in the given tree.
    /// </summary>
    public float GetPoints(EntityUid target,
        ProtoId<CESkillTreePrototype> tree,
        CESkillTreeLearningComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return 0f;

        component.Points.TryGetValue(tree, out var points);
        return points;
    }

    /// <summary>
    /// Grants the entity access to learn nodes from the given skill tree.
    /// </summary>
    public bool TryAddSkillTree(EntityUid target,
        ProtoId<CESkillTreePrototype> tree,
        CESkillTreeLearningComponent? component = null)
    {
        if (!_net.IsServer)
            return false;

        component ??= EnsureComp<CESkillTreeLearningComponent>(target);

        if (!component.AvailableTrees.Add(tree))
            return false;

        DirtyField(target, component, nameof(CESkillTreeLearningComponent.AvailableTrees));

        var ev = new CESkillTreeUnlocked(target, tree);
        RaiseLocalEvent(target, ref ev);

        return true;
    }

    /// <summary>
    /// Revokes the entity's access to the given skill tree.
    /// </summary>
    public bool TryRemoveSkillTree(EntityUid target,
        ProtoId<CESkillTreePrototype> tree,
        CESkillTreeLearningComponent? component = null)
    {
        if (!_net.IsServer)
            return false;

        if (!Resolve(target, ref component, false))
            return false;

        if (!component.AvailableTrees.Remove(tree))
            return false;

        DirtyField(target, component, nameof(CESkillTreeLearningComponent.AvailableTrees));

        var ev = new CESkillTreeLocked(target, tree);
        RaiseLocalEvent(target, ref ev);

        return true;
    }

    /// <summary>
    /// Grants the entity points to spend in the given skill tree.
    /// </summary>
    public bool TryAddSkillTreePoints(EntityUid target,
        ProtoId<CESkillTreePrototype> tree,
        float points,
        CESkillTreeLearningComponent? component = null)
    {
        if (!_net.IsServer)
            return false;

        component ??= EnsureComp<CESkillTreeLearningComponent>(target);

        component.Points.TryGetValue(tree, out var current);
        component.Points[tree] = current + points;
        DirtyField(target, component, nameof(CESkillTreeLearningComponent.Points));

        return true;
    }

    /// <summary>
    /// Takes points away from the entity's balance in the given skill tree.
    /// </summary>
    public bool TryRemoveSkillTreePoints(EntityUid target,
        ProtoId<CESkillTreePrototype> tree,
        float points,
        CESkillTreeLearningComponent? component = null)
    {
        if (!_net.IsServer)
            return false;

        if (!Resolve(target, ref component, false))
            return false;

        if (!component.Points.TryGetValue(tree, out var current))
            return false;

        component.Points[tree] = MathF.Max(current - points, 0f);
        DirtyField(target, component, nameof(CESkillTreeLearningComponent.Points));

        return true;
    }

    /// <summary>
    /// Client-side: asks the server to learn the given node of the given tree.
    /// </summary>
    public void RequestLearnSkillTreeNode(EntityUid target, ProtoId<CESkillTreePrototype> tree, ProtoId<CESkillPrototype> skill)
    {
        RaiseNetworkEvent(new CETryLearnSkillTreeNodeMessage(GetNetEntity(target), tree, skill));

        // Optimistic feedback - played immediately client-side, same as the pre-removal skill tree UI did,
        // rather than waiting for the server to confirm the learn actually succeeded.
        if (_proto.TryIndex(tree, out var treeProto))
            _audio.PlayGlobal(treeProto.LearnSound, target, AudioParams.Default.WithVolume(6f));
    }

    [SubscribeNetworkEvent]
    private void OnTryLearnSkillTreeNode(CETryLearnSkillTreeNodeMessage msg, EntitySessionEventArgs args)
    {
        if (!_net.IsServer)
            return;

        var target = GetEntity(msg.Entity);

        // Players may only learn skills for their own attached entity.
        if (args.SenderSession.AttachedEntity != target)
            return;

        TryLearnSkillTreeNode(target, msg.Tree, msg.Skill);
    }

    /// <summary>
    /// Validates and, if allowed, learns the given node: the tree must be available to the entity,
    /// the node's parent (if any) must already be learned, the entity must have enough points, and
    /// <see cref="CESharedSkillSystem.TryAddSkill"/> must accept the skill (not already known, its
    /// own <see cref="CESkillPrototype.Conditions"/> met). Spends the node's cost on success.
    /// </summary>
    public bool TryLearnSkillTreeNode(EntityUid target,
        ProtoId<CESkillTreePrototype> tree,
        ProtoId<CESkillPrototype> skill,
        CESkillTreeLearningComponent? component = null)
    {
        if (!_net.IsServer)
            return false;

        if (!Resolve(target, ref component, false))
            return false;

        if (!component.AvailableTrees.Contains(tree))
            return false;

        if (!_proto.TryIndex(tree, out var treeProto))
            return false;

        var node = FindNode(treeProto.Nodes, skill, null, out var parentSkill);
        if (node == null)
            return false;

        if (parentSkill is { } parent && !_skill.HaveSkill(target, parent))
            return false;

        component.Points.TryGetValue(tree, out var points);
        if (points < node.Cost)
            return false;

        if (!_skill.TryAddSkill(target, skill))
            return false;

        component.Points[tree] = points - node.Cost;
        DirtyField(target, component, nameof(CESkillTreeLearningComponent.Points));

        return true;
    }

    private static CESkillTreeNode? FindNode(List<CESkillTreeNode> nodes,
        ProtoId<CESkillPrototype> skill,
        ProtoId<CESkillPrototype>? parent,
        out ProtoId<CESkillPrototype>? parentSkill)
    {
        foreach (var node in nodes)
        {
            if (node.Skill == skill)
            {
                parentSkill = parent;
                return node;
            }

            var found = FindNode(node.Children, skill, node.Skill, out parentSkill);
            if (found != null)
                return found;
        }

        parentSkill = null;
        return null;
    }
}

/// <summary>
/// Raised by the client to ask the server to learn a skill tree node.
/// </summary>
[Serializable, NetSerializable]
public sealed class CETryLearnSkillTreeNodeMessage(NetEntity entity, ProtoId<CESkillTreePrototype> tree, ProtoId<CESkillPrototype> skill) : EntityEventArgs
{
    public readonly NetEntity Entity = entity;
    public readonly ProtoId<CESkillTreePrototype> Tree = tree;
    public readonly ProtoId<CESkillPrototype> Skill = skill;
}

/// <summary>
/// Raised on an entity when it gains access to a skill tree.
/// </summary>
[ByRefEvent]
public record struct CESkillTreeUnlocked(EntityUid Entity, ProtoId<CESkillTreePrototype> Tree);

/// <summary>
/// Raised on an entity when it loses access to a skill tree.
/// </summary>
[ByRefEvent]
public record struct CESkillTreeLocked(EntityUid Entity, ProtoId<CESkillTreePrototype> Tree);
