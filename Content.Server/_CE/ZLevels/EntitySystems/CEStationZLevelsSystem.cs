using Content.Server._CE.ZLevels.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Maps;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEStationZLevelsSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializePortals();
        InitActions();

        SubscribeLocalEvent<CEStationZLevelsComponent, StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(Entity<CEStationZLevelsComponent> ent, ref StationPostInitEvent args)
    {
        if (ent.Comp.Initialized)
            return;

        var defaultMap = _station.GetLargestGrid(ent.Owner);
        if (defaultMap is null)
        {
            Log.Error($"Failed to init CEStationZLevelsSystem: defaultMap is null");
            return;
        }

        ent.Comp.LevelEntities.Add(Transform(defaultMap.Value).MapID, ent.Comp.DefaultMapLevel);

        ent.Comp.Initialized = true;

        foreach (var (map, level) in ent.Comp.Levels)
        {
            if (ent.Comp.LevelEntities.ContainsValue(map))
            {
                Log.Error($"Key duplication for CEStationZLevelsSystem at level {map}!");
                continue;
            }

            if (level.Path is null)
            {
                Log.Error($"path {level.Path.ToString()} for CEStationZLevelsSystem at level {map} don't exist!");
                continue;
            }

            //var mapUid = _map.CreateMap(out var mapId);


            if (!_mapLoader.TryLoadMap(level.Path.Value, out var mapEnt, out var grids))
            {
                Log.Error($"Failed to load map for CEStationZLevelsSystem at level {map}!");
                continue;
            }

            Log.Info($"Created map {mapEnt.Value.Comp.MapId} for CEStationZLevelsSystem at level {map}");

            _map.InitializeMap(mapEnt.Value.Comp.MapId);
            var member = EnsureComp<StationMemberComponent>(mapEnt.Value);
            member.Station = ent;

            ent.Comp.LevelEntities.Add(mapEnt.Value.Comp.MapId, map);
        }
    }

    public MapId? GetMapOffset(EntityUid mapUid, int offset)
    {
        var query = EntityQueryEnumerator<CEStationZLevelsComponent>();
        while (query.MoveNext(out var uid, out var zLevel))
        {
            if (!zLevel.LevelEntities.TryGetValue(Transform(mapUid).MapID, out var currentLevel))
                continue;

            var targetLevel = currentLevel + offset;

            if (!zLevel.LevelEntities.ContainsValue(targetLevel))
                continue;

            foreach (var (key, value) in zLevel.LevelEntities)
            {
                if (value == targetLevel && _map.MapExists(key))
                    return key;
            }
        }
        return null;
    }
}
