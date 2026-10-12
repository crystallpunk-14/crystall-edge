using System.Diagnostics;
using Content.Server._CE.Procedural.Generation;
using Content.Server._CE.Procedural.Generation.Points;
using Content.Server._CE.Procedural.Wildlands.Components;
using Content.Server._CE.ZLevels.Core;
using Content.Server._CE.ZLevels.Core.Components;
using Content.Server.Decals;
using Content.Server.Parallax;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.CCVar;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Light.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Procedural.Wildlands;

/// <summary>
/// Generates wildlands around a station's mapped city on every z-level, then rolls wildlands and city modifiers.
/// </summary>
public sealed partial class CEWildlandsSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IComponentFactory _compFactory = default!;
    [Dependency] private ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private BiomeSystem _biome = default!;
    [Dependency] private DecalSystem _decals = default!;
    [Dependency] private SharedRoofSystem _roof = default!;

    [SubscribeLocalEvent(after: [typeof(CEZLevelsSystem)])]
    private void OnStationPostInit(Entity<CEStationWildlandsComponent> ent, ref StationPostInitEvent args)
    {
        var config = _proto.Index(ent.Comp.Config);
        var levels = GetLevels(ent);
        if (levels.Count == 0)
        {
            Log.Error($"Station {ToPrettyString(ent)} has no grid maps to generate wildlands around.");
            return;
        }

        var seed = _cfg.GetCVar(CCVars.CEWildlandsSeed);
        if (seed == 0)
            seed = _random.Next();

        var stopwatch = Stopwatch.StartNew();

        var city = GetCityTiles(levels);
        var points = CEProceduralPoint.Resolve(config.Points, config.Center, seed);
        var wildContext = CreateContext(seed, config.Center, new CEProceduralRegion(city, inside: false), points);
        var cityContext = wildContext with { Region = new CEProceduralRegion(city, inside: true) };

        var depths = new List<int>(config.LayersByDepth.Keys);
        depths.Sort();
        depths.Reverse();
        foreach (var depth in depths)
        {
            if (!levels.TryGetValue(depth, out var map))
            {
                Log.Error($"Wildlands config {config.ID} has layers for depth {depth}, which station {ToPrettyString(ent)} doesn't have.");
                continue;
            }

            foreach (var layer in config.LayersByDepth[depth])
            {
                layer.Apply(wildContext, map);
            }
        }

        ApplyModifiers(config.WildlandsModifiers, wildContext, levels);
        ApplyModifiers(config.CityModifiers, cityContext with { Seed = seed + 1 }, levels);

        Log.Info($"Generated wildlands {config.ID} for {ToPrettyString(ent)} with seed {seed} in {stopwatch.Elapsed.TotalSeconds:0.00}s.");
    }

    private CEProceduralGenerationContext CreateContext(
        int seed,
        Vector2i center,
        CEProceduralRegion region,
        IReadOnlyDictionary<string, List<Vector2i>> points)
    {
        return new CEProceduralGenerationContext(
            EntityManager,
            _map,
            _biome,
            _proto,
            _tileDefManager,
            _decals,
            _roof,
            seed,
            center,
            region,
            points);
    }

    private Dictionary<int, EntityUid> GetLevels(Entity<CEStationWildlandsComponent> station)
    {
        var levels = new Dictionary<int, EntityUid>();

        if (TryComp<CEStationZLevelsComponent>(station, out var stationZLevels) &&
            TryComp<CEZMapNetworkComponent>(stationZLevels.ZNetworkEntity, out var network))
        {
            foreach (var (depth, mapUid) in network.ZLevels)
            {
                if (mapUid is { } uid && HasComp<MapGridComponent>(uid))
                    levels[depth] = uid;
            }

            return levels;
        }

        // Single-level stations never get a z-network.
        if (_station.GetLargestGrid(station.Owner) is { } grid &&
            Transform(grid).MapUid is { } mainMap &&
            HasComp<MapGridComponent>(mainMap))
        {
            levels[0] = mainMap;
        }

        return levels;
    }

    /// <summary>
    /// Union of every level's tiles plus the empty pockets they fully enclose.
    /// </summary>
    private HashSet<Vector2i> GetCityTiles(Dictionary<int, EntityUid> levels)
    {
        var city = new HashSet<Vector2i>();
        foreach (var map in levels.Values)
        {
            var grid = Comp<MapGridComponent>(map);
            var tiles = _map.GetAllTilesEnumerator(map, grid);
            while (tiles.MoveNext(out var tileRef))
            {
                city.Add(tileRef.Value.GridIndices);
            }
        }

        if (city.Count == 0)
            return city;

        var min = new Vector2i(int.MaxValue, int.MaxValue);
        var max = new Vector2i(int.MinValue, int.MinValue);
        foreach (var tile in city)
        {
            min = Vector2i.ComponentMin(min, tile);
            max = Vector2i.ComponentMax(max, tile);
        }

        // One tile of margin guarantees the flood fill can walk all the way around the city.
        min -= Vector2i.One;
        max += Vector2i.One;

        var outside = new HashSet<Vector2i> { min };
        var frontier = new Queue<Vector2i>();
        frontier.Enqueue(min);
        while (frontier.TryDequeue(out var tile))
        {
            foreach (var dir in new[] { Vector2i.Up, Vector2i.Down, Vector2i.Left, Vector2i.Right })
            {
                var next = tile + dir;
                if (next.X < min.X || next.Y < min.Y || next.X > max.X || next.Y > max.Y)
                    continue;

                if (city.Contains(next) || !outside.Add(next))
                    continue;

                frontier.Enqueue(next);
            }
        }

        for (var x = min.X; x <= max.X; x++)
        {
            for (var y = min.Y; y <= max.Y; y++)
            {
                var tile = new Vector2i(x, y);
                if (!outside.Contains(tile))
                    city.Add(tile);
            }
        }

        return city;
    }

    private void ApplyModifiers(EntityTableSelector? table, CEProceduralGenerationContext context, Dictionary<int, EntityUid> levels)
    {
        if (table is null)
            return;

        IRobustRandom random = new RobustRandom();
        random.SetSeed(context.Seed);

        var index = 0;
        foreach (var protoId in _entityTable.GetSpawns(table, random))
        {
            index++;
            if (!_proto.Index(protoId).TryGetComponent<CEWildlandsModifierComponent>(out var modifier, _compFactory))
            {
                Log.Error($"Wildlands modifier table picked {protoId}, which has no {nameof(CEWildlandsModifierComponent)}.");
                continue;
            }

            // Each pick gets its own seed so two modifiers never roll identical noise or shuffles.
            var modifierContext = context with { Seed = context.Seed + index * 7919 };
            foreach (var effect in modifier.Effects)
            {
                effect.Apply(modifierContext, levels);
            }
        }
    }
}
