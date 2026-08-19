using Content.Server.Actions;
using Content.Shared._CE.Waypointer;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Waypointer;

/// <summary>
/// This handles the PVSOverrides for the Waypointer System.
/// </summary>
public sealed partial class CEWaypointerSystem : CESharedWaypointerSystem
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private ActionsSystem  _actions = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEWaypointerComponent, ComponentInit>(OnAddition);
        SubscribeLocalEvent<CEWaypointerComponent, ComponentRemove>(OnRemoval);

        SubscribeLocalEvent<CEWaypointerTrackableComponent, ComponentInit>(OnTrackableInit);

        SubscribeLocalEvent<CEWaypointerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CEWaypointerComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<CEWaypointerComponent, MapUidChangedEvent>(OnMapChanged);
    }

    private void OnAddition(Entity<CEWaypointerComponent> player, ref ComponentInit args)
    {
        _actions.AddAction(player, ref player.Comp.ActionEntity, player.Comp.ActionProtoId);
        AddOverrides(player);
    }

    private void OnRemoval(Entity<CEWaypointerComponent> player, ref ComponentRemove args)
    {
        _actions.RemoveAction(player.Owner, player.Comp.ActionEntity);
        RemoveOverrides(player);
    }

    private void OnTrackableInit(Entity<CEWaypointerTrackableComponent> trackable, ref ComponentInit args)
    {
        var waypointerQuery = EntityQueryEnumerator<CEWaypointerComponent>();

        while (waypointerQuery.MoveNext(out var uid, out var comp))
        {
            RefreshOverrides((uid, comp));
        }
    }

    private void OnPlayerAttached(Entity<CEWaypointerComponent> player, ref PlayerAttachedEvent args)
    {
        AddOverrides(player);
    }

    private void OnPlayerDetached(Entity<CEWaypointerComponent> player, ref PlayerDetachedEvent args)
    {
        RemoveOverrides(player);
    }

    private void OnMapChanged(Entity<CEWaypointerComponent> player, ref MapUidChangedEvent args)
    {
        // Since we only override PVS on entities on the same map, if the person switches maps, they'll need new overrides.
        RefreshOverrides(player);
    }

    /// <summary>
    /// Refreshes the Waypointer PVS Overiddes for an entity if they are controlled by a player.
    /// </summary>
    /// <param name="player">The entity to have their overrides refreshed.</param>
    [PublicAPI]
    public void RefreshOverrides(Entity<CEWaypointerComponent> player)
    {
        RemoveOverrides(player);
        AddOverrides(player);
    }

    private void RemoveOverrides(Entity<CEWaypointerComponent> player)
    {
        if (!_player.TryGetSessionByEntity(player, out var session))
            return;

        foreach (var waypointerProtoId in player.Comp.WaypointerProtoIds)
        {
            if (!_prototype.Resolve(waypointerProtoId, out var prototype))
                continue;

            var waypointQuery = _entity.CompRegistryQueryEnumerator(prototype.TrackedComponents);
            while (waypointQuery.MoveNext(out var target))
            {
                // Entities with Mapgrids somehow already work, so we exclude them. No idea why. But I fear messing with them.
                if (HasComp<MapGridComponent>(target))
                    continue;

                _pvsOverride.RemoveSessionOverride(target, session);
            }
        }
    }

    private void AddOverrides(Entity<CEWaypointerComponent> player)
    {
        if (!_player.TryGetSessionByEntity(player, out var session))
            return;

        var playerXform = Transform(player);

        foreach (var waypointerProtoId in player.Comp.WaypointerProtoIds)
        {
            if (!_prototype.Resolve(waypointerProtoId, out var prototype))
                continue;

            var waypointQuery = _entity.CompRegistryQueryEnumerator(prototype.TrackedComponents);
            while (waypointQuery.MoveNext(out var target))
            {
                // Check if the target fails/passes the whitelist/blacklist.
                if (_whitelist.IsWhitelistFail(prototype.Whitelist, target)
                    || _whitelist.IsWhitelistPass(prototype.Blacklist, target))
                    continue;

                // Entities with Mapgrids somehow already work, so we exclude them. No idea why. But I fear messing with them.
                if (HasComp<MapGridComponent>(target))
                    continue;

                var targetXform = Transform(target);

                // Check if they're in the same Map. If not, don't override.
                if (targetXform.MapID == playerXform.MapID)
                    _pvsOverride.AddSessionOverride(target, session);
            }
        }
    }
}
