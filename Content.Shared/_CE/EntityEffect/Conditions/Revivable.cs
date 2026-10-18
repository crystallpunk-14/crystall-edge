using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity is dead and its player's soul has not left the body for good.
/// </summary>
public sealed partial class Revivable : CEEntityConditionBase<Revivable>
{
    public override string GetDescription(IEntityManager entityManager, IPrototypeManager prototype) =>
        Loc.GetString("ce-revivable-required");
}

public sealed partial class CERevivableConditionSystem : CEEntityConditionSystem<Revivable>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    [Dependency] private EntityQuery<MindContainerComponent> _mindContainerQuery = default!;
    [Dependency] private EntityQuery<UnrevivableComponent> _unrevivableQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<Revivable> args)
    {
        args.Result = IsRevivable(args.Entity);
    }

    /// <summary>
    /// Dead, revivable, and still holding a player's mind - the inverse of the "irrecoverable" examine state.
    /// </summary>
    public bool IsRevivable(EntityUid uid)
    {
        if (!_mobState.IsDead(uid) || _unrevivableQuery.HasComp(uid))
            return false;

        if (!_mindContainerQuery.TryComp(uid, out var container) || !container.HasMind)
            return false;

        // The mind itself is only networked to its owner, so the client trusts HasMind.
        if (_net.IsClient)
            return true;

        return _mind.TryGetMind(uid, out _, out var mind, container) && mind.UserId != null;
    }
}
