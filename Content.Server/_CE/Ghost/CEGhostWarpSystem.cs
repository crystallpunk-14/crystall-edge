using System.Linq;
using Content.Server._CE.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared._CE.Ghost;
using Content.Shared._CE.Roles;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Warps;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Ghost;

/// <summary>
/// CE-owned copy of Content.Server.Ghost.GhostSystem's warp list building (GetPlayerWarps/GetLocationWarps),
/// kept separate so the response (<see cref="CEGhostWarp"/>) can carry CE-specific fields without
/// touching the upstream system. Warping to the chosen target still goes through the vanilla
/// GhostWarpToTargetRequestEvent pipeline - only the list building is duplicated here.
/// </summary>
public sealed partial class CEGhostWarpSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private JobSystem _jobs = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private CESecretRoleInfoSystem _secretRoleInfo = default!;
    [Dependency] private CEGhostMetaSystem _ghostMeta = default!;

    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CEGhostWarpsRequestEvent>(OnWarpsRequest);
    }

    private void OnWarpsRequest(CEGhostWarpsRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } entity || !_ghostQuery.HasComp(entity))
        {
            Log.Warning($"User {args.SenderSession.Name} sent a {nameof(CEGhostWarpsRequestEvent)} without being a ghost.");
            return;
        }

        SendWarps(entity, args.SenderSession);
    }

    /// <summary>
    /// Refreshes an open warp menu when the ghost gains or loses access to meta information.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMetaAccessChanged(ref CEGhostMetaAccessChangedEvent args)
    {
        if (_player.TryGetSessionByEntity(args.Ghost, out var session))
            SendWarps(args.Ghost, session);
    }

    private void SendWarps(EntityUid ghost, ICommonSession session)
    {
        var canSeeMeta = _ghostMeta.CanSeeMeta(ghost);
        var response = new CEGhostWarpsResponseEvent(GetPlayerWarps(ghost, canSeeMeta).Concat(GetLocationWarps()).ToList(), canSeeMeta);
        RaiseNetworkEvent(response, session.Channel);
    }

    private IEnumerable<CEGhostWarp> GetLocationWarps()
    {
        var query = AllEntityQuery<WarpPointComponent>();

        while (query.MoveNext(out var uid, out var warp))
        {
            yield return new CEGhostWarp(GetNetEntity(uid), warp.Location == null ? Name(uid) : Loc.GetString(warp.Location), true);
        }
    }

    private IEnumerable<CEGhostWarp> GetPlayerWarps(EntityUid except, bool canSeeMeta)
    {
        foreach (var player in _player.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } attached || attached == except)
                continue;

            TryComp<MindContainerComponent>(attached, out var mind);

            _jobs.MindTryGetJobId(mind?.Mind, out var jobId);
            ProtoId<CESecretRolePrototype>? secretRole = null;
            ProtoId<CESecretRolePrototype>? originalSecretRole = null;
            if (canSeeMeta)
                _secretRoleInfo.TryGetSecretRole(attached, out secretRole, out originalSecretRole);

            var playerName = Comp<MetaDataComponent>(attached).EntityName;

            if (_mobState.IsAlive(attached) || _mobState.IsCritical(attached))
                yield return new CEGhostWarp(GetNetEntity(attached), playerName, false, jobId, secretRole, originalSecretRole);
        }
    }
}
