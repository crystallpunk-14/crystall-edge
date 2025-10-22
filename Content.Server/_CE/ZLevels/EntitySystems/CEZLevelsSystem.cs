using System.Diagnostics.CodeAnalysis;
using Content.Server._CE.PVS;
using Content.Server._CE.ZLevels.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared._CE.ZLevels;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEZLevelsSystem : CESharedZLevelsSystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializePortals(); //Delete and replace with some generic Z-level movements
        InitActions();
        InitView();
        InitAPI();

        SubscribeLocalEvent<CEStationZLevelsComponent, StationPostInitEvent>(OnStationPostInit);
    }

    public Entity<CEZLevelsComponent> CreateZNetwork()
    {
        var ent = Spawn();

        var zLevel = EnsureComp<CEZLevelsComponent>(ent);
        EnsureComp<CEPvsOverrideComponent>(ent);

        return (ent, zLevel);
    }

    public bool TryAddMapIntoZNetwork(Entity<CEZLevelsComponent> network, MapId mapId, int depth)
    {
        if (network.Comp.ZLevels.ContainsKey(mapId))
        {
            Log.Error($"Failed to add map {mapId} to ZLevelNetwork {network}: This map is already in this network.");
            return false;
        }

        if (TryGetZNetwork(mapId, out _, out var otherNetwork))
        {
            Log.Error($"Failed attempt to add map {mapId} to ZLevelNetwork {network}: This map is already in another network {otherNetwork}.");
            return false;
        }

        if (network.Comp.ZLevels.ContainsValue(depth))
        {
            Log.Error($"Failed attempt to add map {mapId} to ZLevelNetwork {network} at depth {depth}: This depth is already occupied.");
            return false;
        }

        network.Comp.ZLevels.Add(mapId, depth);
        return true;
    }

    /// <summary>
    /// Checks whether the map is in the zLevels network. If so, returns true and the current depth + Entity of the current zLevels network.
    /// </summary>
    public bool TryGetZNetwork(MapId mapId,[NotNullWhen(true)] out int? depth, [NotNullWhen(true)] out Entity<CEZLevelsComponent>? zLevel)
    {
        depth = null;
        zLevel = null;
        var query = EntityQueryEnumerator<CEZLevelsComponent>();
        while (query.MoveNext(out var uid, out var zLevelComp))
        {
            if (zLevelComp.ZLevels.TryGetValue(mapId, out var foundedDepth))
            {
                depth = foundedDepth;
                zLevel = (uid, zLevelComp);
                return true;
            }
        }

        return false;
    }

    private void OnStationPostInit(Entity<CEStationZLevelsComponent> ent, ref StationPostInitEvent args)
    {
        if (ent.Comp.ZLevelsInitialized)
            return;

        var defaultMap = _station.GetLargestGrid(ent.Owner);
        if (defaultMap is null)
        {
            Log.Error($"Failed to init CEStationZLevelsSystem: defaultMap is null");
            return;
        }

        var stationNetwork = CreateZNetwork();

        TryAddMapIntoZNetwork(stationNetwork, Transform(defaultMap.Value).MapID, ent.Comp.DefaultMapLevel);

        ent.Comp.ZLevelsInitialized = true;

        foreach (var (depth, map) in ent.Comp.Levels)
        {
            if (map.Path is null)
            {
                Log.Error($"path {map.Path.ToString()} for CEStationZLevelsSystem at level {depth} don't exist!");
                continue;
            }

            if (!_mapLoader.TryLoadMap(map.Path.Value, out var mapEnt, out _))
            {
                Log.Error($"Failed to load map for Station ZLevelNetwork at depth {depth}!");
                continue;
            }

            Log.Info($"Created map {mapEnt.Value.Comp.MapId} for CEStationZLevelsSystem at level {depth}");

            _map.InitializeMap(mapEnt.Value.Comp.MapId);
            var member = EnsureComp<StationMemberComponent>(mapEnt.Value);
            member.Station = ent;

            TryAddMapIntoZNetwork(stationNetwork, mapEnt.Value.Comp.MapId, depth);
        }
    }
}
