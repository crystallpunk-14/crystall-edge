using System.Linq;
using Content.Server.Roles.Jobs;
using Content.Shared._CE.Ghost;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Warps;
using Robust.Shared.Player;

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

        var response = new CEGhostWarpsResponseEvent(GetPlayerWarps(entity).Concat(GetLocationWarps()).ToList());
        RaiseNetworkEvent(response, args.SenderSession.Channel);
    }

    private IEnumerable<CEGhostWarp> GetLocationWarps()
    {
        var query = AllEntityQuery<WarpPointComponent>();

        while (query.MoveNext(out var uid, out var warp))
        {
            yield return new CEGhostWarp(GetNetEntity(uid), warp.Location == null ? Name(uid) : Loc.GetString(warp.Location), true);
        }
    }

    private IEnumerable<CEGhostWarp> GetPlayerWarps(EntityUid except)
    {
        foreach (var player in _player.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } attached || attached == except)
                continue;

            TryComp<MindContainerComponent>(attached, out var mind);

            var jobName = _jobs.MindTryGetJobName(mind?.Mind);
            var playerInfo = $"{Comp<MetaDataComponent>(attached).EntityName} ({jobName})";

            if (_mobState.IsAlive(attached) || _mobState.IsCritical(attached))
                yield return new CEGhostWarp(GetNetEntity(attached), playerInfo, false);
        }
    }
}
