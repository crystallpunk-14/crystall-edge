using System.Linq;
using Content.Server._CE.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared._CE.Character;
using Content.Shared._CE.Ghost;
using Content.Shared._CE.Roles;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
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
    [Dependency] private CECharacterSystem _character = default!;

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
        var response = new CEGhostWarpsResponseEvent(GetPlayerWarps(canSeeMeta).Concat(GetLocationWarps()).ToList(), canSeeMeta);
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

    private IEnumerable<CEGhostWarp> GetPlayerWarps(bool canSeeMeta)
    {
        // Every character that ever entered the round, whatever happened to it since.
        var characters = EntityQueryEnumerator<CECharacterComponent, MindComponent>();
        while (characters.MoveNext(out var mindId, out var character, out var mind))
        {
            var status = _character.GetStatus((mindId, character, mind));
            var body = character.Body is { } b && !TerminatingOrDeleted(b) ? b : (EntityUid?) null;
            var name = body is { } named ? Name(named) : mind.CharacterName ?? string.Empty;

            ProtoId<CESecretRolePrototype>? secretRole = null;
            ProtoId<CESecretRolePrototype>? originalSecretRole = null;
            if (canSeeMeta)
                _secretRoleInfo.TryGetMindSecretRole((mindId, mind), out secretRole, out originalSecretRole);

            var target = GetWarpTarget(status, body, mind);
            yield return new CEGhostWarp(target is { } t ? GetNetEntity(t) : NetEntity.Invalid,
                name,
                false,
                character.Job,
                secretRole,
                originalSecretRole,
                status);
        }

        // Living players that aren't characters, e.g. ghost roles.
        foreach (var player in _player.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } attached ||
                _ghostQuery.HasComp(attached) ||
                HasComp<CECharacterBodyComponent>(attached) ||
                !(_mobState.IsAlive(attached) || _mobState.IsCritical(attached)))
            {
                continue;
            }

            TryComp<MindContainerComponent>(attached, out var mindContainer);
            _jobs.MindTryGetJobId(mindContainer?.Mind, out var jobId);

            ProtoId<CESecretRolePrototype>? secretRole = null;
            ProtoId<CESecretRolePrototype>? originalSecretRole = null;
            if (canSeeMeta)
                _secretRoleInfo.TryGetSecretRole(attached, out secretRole, out originalSecretRole);

            yield return new CEGhostWarp(GetNetEntity(attached), Name(attached), false, jobId, secretRole, originalSecretRole);
        }
    }

    /// <summary>
    /// The body if it's still in the round, otherwise wherever the player is now (usually their ghost).
    /// </summary>
    private EntityUid? GetWarpTarget(CECharacterStatus status, EntityUid? body, MindComponent mind)
    {
        if (body != null && status != CECharacterStatus.LeftRound)
            return body;

        if (mind.CurrentEntity is { } current && current != body && !TerminatingOrDeleted(current))
            return current;

        return null;
    }
}
