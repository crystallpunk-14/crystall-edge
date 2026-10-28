using Content.Shared._CE.Waypointer;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Waypointer;

/// <summary>
/// Keeps PVS session overrides on the specific tracked entities of waypointers with
/// <see cref="CEWaypointerPrototype.PvsOverride"/>, so they can be pointed at from afar.
/// </summary>
public sealed partial class CEWaypointerSystem : CESharedWaypointerSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;

    protected override void OnRemoval(Entity<CEWaypointerComponent> player, ref ComponentRemove args)
    {
        base.OnRemoval(player, ref args);

        if (_player.TryGetSessionByEntity(player, out var session))
            ClearOverrides(player, session);
    }

    protected override void OnTrackedEntitiesChanged(Entity<CEWaypointerComponent> player)
    {
        base.OnTrackedEntitiesChanged(player);

        UpdateOverrides(player);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<CEWaypointerComponent> player, ref PlayerAttachedEvent args)
    {
        UpdateOverrides(player);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<CEWaypointerComponent> player, ref PlayerDetachedEvent args)
    {
        ClearOverrides(player, args.Player);
    }

    private void UpdateOverrides(Entity<CEWaypointerComponent> player)
    {
        if (!_player.TryGetSessionByEntity(player, out var session))
            return;

        var desired = new HashSet<EntityUid>();
        foreach (var (target, protoId) in player.Comp.TrackedEntities)
        {
            if (_proto.Resolve(protoId, out var proto) && proto.PvsOverride && !TerminatingOrDeleted(target))
                desired.Add(target);
        }

        foreach (var old in player.Comp.PvsOverrides)
        {
            if (!desired.Contains(old) && !TerminatingOrDeleted(old))
                _pvsOverride.RemoveSessionOverride(old, session);
        }

        foreach (var target in desired)
        {
            if (!player.Comp.PvsOverrides.Contains(target))
                _pvsOverride.AddSessionOverride(target, session);
        }

        player.Comp.PvsOverrides = desired;
    }

    private void ClearOverrides(Entity<CEWaypointerComponent> player, ICommonSession session)
    {
        foreach (var target in player.Comp.PvsOverrides)
        {
            if (!TerminatingOrDeleted(target))
                _pvsOverride.RemoveSessionOverride(target, session);
        }

        player.Comp.PvsOverrides.Clear();
    }
}
