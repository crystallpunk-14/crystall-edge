using System.Linq;
using System.Threading;
using Content.Server._CE.Demiplane.Components;
using Content.Server._CE.Demiplane.Prototypes;
using Content.Server._CE.ZLevels.Core;
using Content.Server._CE.ZLevels.Core.Components;
using Content.Server.Decals;
using Content.Server.Parallax;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Station;
using Robust.Server.GameObjects;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Demiplane;

/// <summary>
/// Orchestrates a station's demiplane teleport: clears the old stage out of the z-network
/// immediately, then — once both a dramatic-pause timer and (if a location was given) background
/// generation are done — merges the new stage in below the island. See
/// <see cref="CEStationDemiplaneTeleportationComponent"/> for the state this hangs off of and
/// <see cref="ICEDemiplaneLocationGenerator"/> for how a location actually gets generated.
/// </summary>
public sealed partial class CEDemiplaneSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private CEZLevelsSystem _zLevels = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private BiomeSystem _biome = default!;
    [Dependency] private ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private DecalSystem _decals = default!;
    [Dependency] private SharedRoofSystem _roof = default!;

    private ISawmill _sawmill = default!;

    private const double JobMaxTime = 0.002;
    private readonly JobQueue _jobQueue = new();

    private readonly Dictionary<EntityUid, (CEDemiplaneGenerationJob Job, CancellationTokenSource Cancel)> _activeJobs = new();

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("ce_demiplane");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _jobQueue.Process();
        CollectFinishedJobs();
        MergeReadyStations();
    }

    /// <summary>
    /// Clears every non-station map out of the station's z-network, then - after
    /// <paramref name="teleportTime"/> - merges in a freshly generated <paramref name="location"/>,
    /// or nothing at all if <paramref name="location"/> is null, leaving the island over the void.
    /// Overwrites (and cancels) any teleport already in progress for this station.
    /// </summary>
    public bool StartTeleport(EntityUid station, ProtoId<CEDemiplaneLocationPrototype>? location, TimeSpan teleportTime)
    {
        if (!TryComp<CEStationZLevelsComponent>(station, out var stationZLevels) ||
            stationZLevels.ZNetworkEntity is not { } networkUid ||
            !TryComp<CEZMapNetworkComponent>(networkUid, out var network))
        {
            _sawmill.Error($"Station {station} has no z-network to teleport.");
            return false;
        }

        if (_activeJobs.Remove(station, out var pending))
            pending.Cancel.Cancel();

        ClearStage(station, (networkUid, network));

        RemComp<CEStationDemiplaneTeleportationComponent>(station);

        var teleport = EnsureComp<CEStationDemiplaneTeleportationComponent>(station);
        teleport.EndTime = _timing.CurTime + teleportTime;
        teleport.Location = location;
        teleport.ReadyMaps = null;

        if (location is null)
        {
            teleport.ReadyMaps = new List<EntityUid>();
            _sawmill.Info($"Station {station}: teleporting to the void, arriving in {teleportTime.TotalSeconds:0}s.");
            return true;
        }

        var locationProto = _proto.Index(location.Value);
        var cancel = new CancellationTokenSource();
        var job = new CEDemiplaneGenerationJob(
            JobMaxTime,
            EntityManager,
            _map,
            _biome,
            _proto,
            _tileDefManager,
            _decals,
            _roof,
            locationProto.Generator,
            _random.Next(),
            cancel.Token);

        _activeJobs[station] = (job, cancel);
        _jobQueue.EnqueueJob(job);

        _sawmill.Info($"Station {station}: generating `{location}`, arriving in {teleportTime.TotalSeconds:0}s.");
        return true;
    }

    private void ClearStage(EntityUid station, Entity<CEZMapNetworkComponent> network)
    {
        if (TryComp<CEStationDemiplaneTeleportationComponent>(station, out var oldTeleport) &&
            oldTeleport.Location is { } oldLocation &&
            _proto.Resolve(oldLocation, out var oldProto))
        {
            foreach (var mapUid in network.Comp.ZLevels.Values)
            {
                if (mapUid is { } uid)
                    EntityManager.RemoveComponents(uid, oldProto.Components);
            }
        }

        var toRemove = new List<EntityUid>();
        foreach (var mapUid in network.Comp.ZLevels.Values)
        {
            if (mapUid is { } uid && _station.GetOwningStation(uid) != station)
                toRemove.Add(uid);
        }

        if (toRemove.Count == 0)
            return;

        _zLevels.TryRemoveMapsFromNetwork(network, toRemove);

        foreach (var uid in toRemove)
        {
            QueueDel(uid);
        }

        _sawmill.Info($"Station {station}: cleared {toRemove.Count} old stage level(s).");
    }

    private void CollectFinishedJobs()
    {
        foreach (var pair in _activeJobs.ToArray())
        {
            var station = pair.Key;
            var job = pair.Value.Job;

            if (job.Status != JobStatus.Finished)
                continue;

            _activeJobs.Remove(station);

            if (TryComp<CEStationDemiplaneTeleportationComponent>(station, out var teleport))
                teleport.ReadyMaps = job.Result ?? new List<EntityUid>();

            if (job.Exception is { } ex)
                _sawmill.Error($"Station {station}: demiplane generation failed: {ex}");
        }
    }

    private void MergeReadyStations()
    {
        var ready = new List<EntityUid>();

        var query = EntityQueryEnumerator<CEStationDemiplaneTeleportationComponent>();
        while (query.MoveNext(out var station, out var teleport))
        {
            if (teleport.ReadyMaps is null || _timing.CurTime < teleport.EndTime)
                continue;

            ready.Add(station);
        }

        foreach (var station in ready)
        {
            if (!TryComp<CEStationDemiplaneTeleportationComponent>(station, out var teleport))
                continue;

            MergeIn(station, teleport);
            teleport.ReadyMaps = null;
        }
    }

    private void MergeIn(EntityUid station, CEStationDemiplaneTeleportationComponent teleport)
    {
        var maps = teleport.ReadyMaps;
        if (maps is null || maps.Count == 0)
        {
            _sawmill.Info($"Station {station}: arrived - nothing below but the void.");
            return;
        }

        if (!TryComp<CEStationZLevelsComponent>(station, out var stationZLevels) ||
            stationZLevels.ZNetworkEntity is not { } networkUid ||
            !TryComp<CEZMapNetworkComponent>(networkUid, out var network))
        {
            _sawmill.Error($"Station {station}: z-network vanished before the generated stage could be merged in.");
            return;
        }

        var startDepth = network.SortedMin - 1;
        var dict = new Dictionary<EntityUid, int>(maps.Count);
        for (var i = 0; i < maps.Count; i++)
        {
            dict[maps[i]] = startDepth - i;
        }

        _zLevels.TryAddMapsIntoNetwork((networkUid, network), dict);

        if (teleport.Location is { } location && _proto.Resolve(location, out var locationProto))
        {
            foreach (var mapUid in network.ZLevels.Values)
            {
                if (mapUid is { } uid)
                    EntityManager.AddComponents(uid, locationProto.Components);
            }
        }

        _sawmill.Info($"Station {station}: merged in {maps.Count} level(s) at depth {startDepth}..{startDepth - maps.Count + 1}.");
    }
}
