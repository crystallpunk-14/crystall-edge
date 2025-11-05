using Content.Server._CE.ZLevels.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared._CE.ZLevels.EntitySystems;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;

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
        InitView();

        SubscribeLocalEvent<CEStationZLevelsComponent, StationPostInitEvent>(OnStationPostInit);
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

        TryAddMapIntoZNetwork(stationNetwork, defaultMap.Value, ent.Comp.DefaultMapLevel);

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

            TryAddMapIntoZNetwork(stationNetwork, mapEnt.Value, depth);
        }
    }
}
