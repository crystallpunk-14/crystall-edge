using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tiles;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._CE.Arrivals;

/// <summary>
/// If enabled spawns players on a separate arrivals station before they can transfer to the main station.
/// </summary>
public sealed class CEArrivalsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    private EntityQuery<CEArrivalsSourceComponent> _arrivalsSourceQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    /// <summary>
    /// If enabled then spawns players on an alternate map so they can take a shuttle to the station.
    /// </summary>
    public bool Enabled { get; private set; }

    /// <summary>
    ///     The first arrival is a little early, to save everyone 10s
    /// </summary>
    private const float RoundStartFTLDuration = 10f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(HandlePlayerSpawning, before: new[] { typeof(SpawnPointSystem) }, after: new[] { typeof(ContainerSpawnPointSystem) });

        SubscribeLocalEvent<CEStationArrivalsComponent, StationPostInitEvent>(OnStationPostInit);
        SubscribeLocalEvent<CEArrivalsShipComponent, ComponentStartup>(OnShuttleStartup);

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);

        _arrivalsSourceQuery = GetEntityQuery<CEArrivalsSourceComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        Enabled = _cfgManager.GetCVar(CCVars.CEArrivalsShuttles);
        _cfgManager.OnValueChanged(CCVars.CEArrivalsShuttles, SetArrivals);
    }

    private void SetArrivals(bool obj)
    {
        Enabled = obj;

        if (Enabled)
        {
            SetupArrivalsStation();
            var query = AllEntityQuery<CEStationArrivalsComponent>();

            while (query.MoveNext(out var sUid, out var comp))
            {
                SetupShuttle((sUid, comp));
            }
        }
        else
        {
            var sourceQuery = AllEntityQuery<CEArrivalsSourceComponent>();

            while (sourceQuery.MoveNext(out var uid, out _))
            {
                QueueDel(uid);
            }

            var shuttleQuery = AllEntityQuery<CEArrivalsShipComponent>();

            while (shuttleQuery.MoveNext(out var uid, out _))
            {
                QueueDel(uid);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEArrivalsShipComponent, ShuttleComponent, TransformComponent>();
        var curTime = _timing.CurTime;
        TryGetArrivals(out var arrivals);

        var arrivalsDockPoint = TryGetArrivalsDockPoint();
        var stationDockPoint = TryGetStationDockPoint();

        if (arrivalsDockPoint is not null && stationDockPoint is not null)
        {
            var arrivalsDockXform = Transform(arrivalsDockPoint.Value);
            var stationDockXform = Transform(stationDockPoint.Value);
            while (query.MoveNext(out var uid, out var comp, out var shuttle, out var xform))
            {
                if (comp.NextTransfer > curTime)
                    continue;

                var tripTime = _shuttles.DefaultTravelTime + _shuttles.DefaultStartupTime;

                // Go back to arrivals source
                if (xform.MapUid != arrivalsDockXform.MapUid)
                {
                    _shuttles.FTLToCoordinates(uid, shuttle, arrivalsDockXform.Coordinates, arrivalsDockXform.LocalRotation);

                    comp.NextArrivalsTime = _timing.CurTime + TimeSpan.FromSeconds(tripTime);
                }
                // Go to station
                else
                {
                    _shuttles.FTLToCoordinates(uid, shuttle, stationDockXform.Coordinates, stationDockXform.LocalRotation);

                    comp.NextArrivalsTime = _timing.CurTime + TimeSpan.FromSeconds(
                        _cfgManager.GetCVar(CCVars.ArrivalsCooldown) + tripTime);
                }

                comp.NextTransfer += TimeSpan.FromSeconds(_cfgManager.GetCVar(CCVars.ArrivalsCooldown));
            }
        }
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        // Setup arrivals station
        if (!Enabled)
            return;

        SetupArrivalsStation();
    }

    private void SetupArrivalsStation()
    {
        var path = new ResPath(_cfgManager.GetCVar(CCVars.ArrivalsMap));

        if (!_loader.TryLoadMap(path, out var map, out _))
            return;

        _metaData.SetEntityName(map.Value, Loc.GetString("ce-arrivals-map-name"));

        EnsureComp<CEArrivalsSourceComponent>(map.Value);
        EnsureComp<ProtectedGridComponent>(map.Value);

        _mapSystem.InitializeMap((map.Value.Owner, map.Value.Comp));

        // Handle roundstart stations.
        var query = AllEntityQuery<CEStationArrivalsComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            SetupShuttle((uid, comp));
        }
    }

    private void HandlePlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.SpawnResult != null)
            return;

        // We use arrivals as the default spawn so don't check for job prio.

        // Only works on latejoin even if enabled.
        if (!Enabled || _ticker.RunLevel != GameRunLevel.InRound)
            return;

        if (!HasComp<CEStationArrivalsComponent>(ev.Station))
            return;

        TryGetArrivals(out var arrivals);

        if (!TryComp(arrivals, out TransformComponent? arrivalsXform))
            return;

        var mapId = arrivalsXform.MapID;

        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();
        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (spawnPoint.SpawnType != SpawnPointType.LateJoin || xform.MapID != mapId)
                continue;

            possiblePositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count <= 0)
            return;

        var spawnLoc = _random.Pick(possiblePositions);
        ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            ev.Job,
            ev.HumanoidCharacterProfile,
            ev.Station);
    }

    private bool TryGetArrivals(out EntityUid uid)
    {
        var arrivalsQuery = EntityQueryEnumerator<CEArrivalsSourceComponent>();

        while (arrivalsQuery.MoveNext(out uid, out _))
        {
            return true;
        }

        return false;
    }

    private EntityUid? TryGetArrivalsDockPoint()
    {
        var dockQuery = EntityQueryEnumerator<CEArrivalsShipDockPointComponent, TransformComponent>();

        while (dockQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (_arrivalsSourceQuery.HasComponent(xform.MapUid))
                return uid;
        }

        return null;
    }

    private EntityUid? TryGetStationDockPoint()
    {
        var dockQuery = EntityQueryEnumerator<CEArrivalsShipDockPointComponent, TransformComponent>();

        while (dockQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (!_arrivalsSourceQuery.HasComponent(xform.MapUid))
                return uid;
        }

        return null;
    }

    private void OnStationPostInit(Entity<CEStationArrivalsComponent> ent, ref StationPostInitEvent args)
    {
        if (!Enabled)
            return;

        // If it's a latespawn station then this will fail but that's okey
        SetupShuttle(ent);
    }

    private void SetupShuttle(Entity<CEStationArrivalsComponent> arrivalsEnt)
    {
        if (!Deleted(arrivalsEnt.Comp.Shuttle))
            return;

        // Spawn arrivals on a dummy map then dock it to the source.
        var dummpMapEntity = _mapSystem.CreateMap(out var dummyMapId);

        var arrivalsDockPoint = TryGetArrivalsDockPoint();
        if (arrivalsDockPoint is not null &&
            _loader.TryLoadGrid(dummyMapId, arrivalsEnt.Comp.ShuttlePath, out var shuttle))
        {
            var dockXform = Transform(arrivalsDockPoint.Value);
            arrivalsEnt.Comp.Shuttle = shuttle.Value;
            var shuttleComp = Comp<ShuttleComponent>(shuttle.Value);
            var ship = EnsureComp<CEArrivalsShipComponent>(shuttle.Value);
            ship.Station = arrivalsEnt;
            EnsureComp<ProtectedGridComponent>(arrivalsEnt);
            _shuttles.FTLToCoordinates(shuttle.Value, shuttleComp, dockXform.Coordinates, dockXform.LocalRotation, hyperspaceTime: RoundStartFTLDuration);
            ship.NextTransfer = _timing.CurTime + TimeSpan.FromSeconds(_cfgManager.GetCVar(CCVars.ArrivalsCooldown));
        }

        // Don't start the arrivals shuttle immediately docked so power has a time to stabilise?
        var timer = AddComp<TimedDespawnComponent>(dummpMapEntity);
        timer.Lifetime = 15f;
    }

    private void OnShuttleStartup(Entity<CEArrivalsShipComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<PreventPilotComponent>(ent);
    }
}
