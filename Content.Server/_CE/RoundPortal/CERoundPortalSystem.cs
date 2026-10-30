using System.Globalization;
using Content.Server._CE.Achievements;
using Content.Server._CE.Roles;
using Content.Server._CE.Statistics;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._CE.Character;
using Content.Shared._CE.RoundPortal;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Ghost.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Station;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._CE.RoundPortal;

/// <summary>
/// Spawns late joiners at a <see cref="CERoundPortalComponent"/> and lets players leave the round through it:
/// their job slot and secret role slot are freed, the body is stored alive on a paused map and the player
/// stays a ghost for the rest of the round.
/// </summary>
public sealed partial class CERoundPortalSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private CECharacterSystem _character = default!;
    [Dependency] private CESecretRoleSelectionSystem _secretRoles = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private StationJobsSystem _stationJobs = default!;
    [Dependency] private StationRecordsSystem _stationRecords = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;

    [SubscribeLocalEvent(before: [typeof(ContainerSpawnPointSystem), typeof(SpawnPointSystem)])]
    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null || _gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        var portals = new List<Entity<CERoundPortalComponent, TransformComponent>>();
        var query = EntityQueryEnumerator<CERoundPortalComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var portal, out var xform))
        {
            if (args.Station != null && _station.GetOwningStation(uid, xform) != args.Station)
                continue;

            portals.Add((uid, portal, xform));
        }

        if (portals.Count == 0)
            return;

        var (portalUid, portalComp, portalXform) = _random.Pick(portals);
        var coords = portalXform.Coordinates.Offset(portalXform.LocalRotation.RotateVec(portalComp.SpawnOffset));

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(coords,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);

        _audio.PlayPvs(portalComp.ArrivalSound, coords);
        if (portalComp.ArrivalEffect != null)
            Spawn(portalComp.ArrivalEffect, coords);

        _adminLog.Add(LogType.LateJoin,
            LogImpact.Low,
            $"{ToPrettyString(args.SpawnResult.Value):player} arrived through the round portal {ToPrettyString(portalUid)}");
    }

    [SubscribeLocalEvent]
    private void OnLeaveMessage(Entity<CERoundPortalComponent> ent, ref CERoundPortalLeaveMessage args)
    {
        var user = args.Actor;
        _ui.CloseUi(ent.Owner, CERoundPortalUiKey.Key, user);

        if (!CanLeave(user))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            ent.Comp.LeaveDelay,
            new CERoundPortalLeaveDoAfterEvent(),
            ent,
            ent)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            BreakOnDamage = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnLeaveDoAfter(Entity<CERoundPortalComponent> ent, ref CERoundPortalLeaveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !CanLeave(args.User))
            return;

        args.Handled = true;
        LeaveRound(ent, args.User);
    }

    [SubscribeLocalEvent]
    private void OnAchievementAttempt(ref CEAchievementAttemptEvent args)
    {
        if (_character.HasLeftRound(args.Player))
            args.Cancelled = true;
    }

    // Statistics gained before leaving are already buffered and stay; only new ones are refused.
    [SubscribeLocalEvent]
    private void OnStatisticAttempt(ref CEStatisticAttemptEvent args)
    {
        if (_character.HasLeftRound(args.Player))
            args.Cancelled = true;
    }

    private bool CanLeave(EntityUid user)
    {
        return _gameTicker.RunLevel == GameRunLevel.InRound &&
               !HasComp<GhostComponent>(user) &&
               _mind.TryGetMind(user, out var mindId, out _) &&
               !HasComp<CERoundLeavedComponent>(mindId);
    }

    private void LeaveRound(Entity<CERoundPortalComponent> portal, EntityUid body)
    {
        if (!_mind.TryGetMind(body, out var mindId, out var mind))
            return;

        var name = Name(body);
        var hasJob = _jobs.MindTryGetJobName(mindId, out var jobName);
        var station = _station.GetOwningStation(portal) ?? _station.GetOwningStation(body);

        if (mind.UserId is { } userId)
        {
            foreach (var uniqueStation in _station.GetStationsSet())
            {
                if (!TryComp<StationJobsComponent>(uniqueStation, out var stationJobs) ||
                    !_stationJobs.TryGetPlayerJobs(uniqueStation, userId, out var jobs, stationJobs))
                    continue;

                foreach (var job in jobs)
                {
                    _stationJobs.TryAdjustJobSlot(uniqueStation, job, 1, clamp: true);
                }

                _stationJobs.TryRemovePlayerJobs(uniqueStation, userId, stationJobs);
            }
        }

        if (station != null &&
            TryComp<StationRecordsComponent>(station, out var stationRecords) &&
            _stationRecords.GetRecordByName(station.Value, name) is { } recordId)
        {
            _stationRecords.RemoveRecord(new StationRecordKey(recordId, station.Value), stationRecords);
        }

        _secretRoles.ReleaseSecretRoleSlot((mindId, mind));
        _character.MarkLeftRound(mindId, body);

        var coords = Transform(body).Coordinates;
        _audio.PlayPvs(portal.Comp.DepartureSound, coords);
        if (portal.Comp.DepartureEffect != null)
            Spawn(portal.Comp.DepartureEffect, coords);

        // The ghost appears where the body stood, then the body is stored alive and frozen.
        _ghost.OnGhostAttempt(mindId, false, forced: true, mind: mind);
        _transform.SetParent(body, EnsureStorageMap());

        if (station != null)
        {
            var message = hasJob
                ? Loc.GetString("ce-round-portal-departure-announcement",
                    ("character", name),
                    ("entity", body),
                    ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName)))
                : Loc.GetString("ce-round-portal-departure-announcement-no-job",
                    ("character", name),
                    ("entity", body));

            _chat.DispatchStationAnnouncement(station.Value,
                message,
                Loc.GetString("ce-round-portal-announcement-sender"),
                playDefaultSound: false);
        }

        _adminLog.Add(LogType.Mind,
            LogImpact.High,
            $"{ToPrettyString(mindId):player} left the round through the round portal {ToPrettyString(portal)} with {ToPrettyString(body):entity}");
    }

    private EntityUid EnsureStorageMap()
    {
        var query = EntityQueryEnumerator<CERoundLeavedStorageMapComponent>();
        if (query.MoveNext(out var existing, out _))
            return existing;

        var mapUid = _map.CreateMap();
        _map.SetPaused(mapUid, true);
        _metaData.SetEntityName(mapUid, Loc.GetString("ce-round-portal-storage-map-name"));
        AddComp<CERoundLeavedStorageMapComponent>(mapUid);
        return mapUid;
    }
}
