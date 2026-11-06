using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity holds a player's mind and that player is connected - in the body or as a ghost.
/// </summary>
public sealed partial class PlayerOnline : CEEntityConditionBase<PlayerOnline>
{
    public override string GetDescription(IEntityManager entityManager, IPrototypeManager prototype) =>
        Loc.GetString(Inverted ? "ce-player-online-forbidden" : "ce-player-online-required");
}

public sealed partial class CEPlayerOnlineConditionSystem : CEEntityConditionSystem<PlayerOnline>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    [Dependency] private EntityQuery<MindContainerComponent> _mindContainerQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<PlayerOnline> args)
    {
        if (!_mindContainerQuery.TryComp(args.Entity, out var container) || !container.HasMind)
            return;

        // The mind itself is only networked to its owner, so the client trusts HasMind.
        if (_net.IsClient)
        {
            args.Result = true;
            return;
        }

        args.Result = _mind.TryGetMind(args.Entity, out _, out var mind, container)
            && mind.UserId is { } userId
            && _player.TryGetSessionById(userId, out var session)
            && session.Status is not (SessionStatus.Disconnected or SessionStatus.Zombie);
    }
}
