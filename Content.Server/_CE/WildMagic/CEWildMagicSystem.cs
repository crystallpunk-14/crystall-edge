using System.Linq;
using Content.Server._CE.ZLevels.Core;
using Content.Server.Station.Systems;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Physics;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.WildMagic;

/// <summary>
/// Generic wild magic node spawning utilities. Doesn't own any pool/lifetime management itself -
/// see <see cref="CEWildMagicRuleSystem"/> for the round's maintained node pool.
/// </summary>
public sealed partial class CEWildMagicSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private StationSystem _stations = default!;
    [Dependency] private CEZLevelsSystem _zLevels = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    private readonly EntProtoId _wildMagicNodeEntity = "CEWildMagicNode";

    /// <summary>
    /// Finds a random valid spot in <paramref name="network"/> and spawns a wild magic node there.
    /// Returns null if no valid spot could be found.
    /// </summary>
    public EntityUid? SpawnNodeInNetwork(Entity<CEZGridNetworkComponent> network)
    {
        if (!TryGetRandomNodeLocation(network, out var coordinates))
            return null;

        return Spawn(_wildMagicNodeEntity, coordinates);
    }

    /// <inheritdoc cref="SpawnNodeInNetwork(Robust.Shared.GameObjects.Entity{Content.Shared._CE.ZLevels.Core.Components.CEZGridNetworkComponent})"/>
    public EntityUid? SpawnNodeInNetwork(Entity<CEZMapNetworkComponent> network)
    {
        if (!TryGetRandomNodeLocation(network, out var coordinates))
            return null;

        return Spawn(_wildMagicNodeEntity, coordinates);
    }

    /// <summary>
    /// Resolves the z-map network of the first found station's largest grid - currently all maps
    /// are planetary (the grid entity is itself the map entity), so this is a map-network lookup
    /// rather than a grid-network one.
    /// </summary>
    public bool TryGetStationNetwork(out Entity<CEZMapNetworkComponent> network)
    {
        network = default;

        var stationQuery = EntityQueryEnumerator<StationDataComponent>();
        if (!stationQuery.MoveNext(out var stationUid, out var stationData))
        {
            Log.Warning("CEWildMagicSystem: no StationDataComponent found in the world.");
            return false;
        }

        if (_stations.GetLargestGrid((stationUid, stationData)) is not { } grid)
        {
            Log.Warning($"CEWildMagicSystem: station {ToPrettyString(stationUid)} has no grids.");
            return false;
        }

        if (!_zLevels.TryGetMapNetwork(grid, out network))
        {
            Log.Warning($"CEWildMagicSystem: grid {ToPrettyString(grid)} (station {ToPrettyString(stationUid)}) isn't part of a z-map network.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to find a random tile suitable for spawning a wild magic node, picking randomly among
    /// every grid (i.e. every z-level floor) belonging to the given z-grid network. Rejects tiles
    /// occupied by a static, hard, impassable anchored entity (walls, etc).
    /// </summary>
    public bool TryGetRandomNodeLocation(Entity<CEZGridNetworkComponent> network, out EntityCoordinates coordinates)
    {
        return TryGetRandomTileOnGrids(network.Comp.Grids.ToList(), out coordinates);
    }

    /// <summary>
    /// Tries to find a random tile suitable for spawning a wild magic node, scanning every map in
    /// the given z-map network but only considering planet maps - ones where the map entity is
    /// itself the grid entity, as opposed to regular multi-grid station maps.
    /// </summary>
    public bool TryGetRandomNodeLocation(Entity<CEZMapNetworkComponent> network, out EntityCoordinates coordinates)
    {
        var planetMaps = new List<EntityUid>();
        foreach (var mapUid in network.Comp.SortedZLevels)
        {
            if (mapUid != EntityUid.Invalid && HasComp<MapGridComponent>(mapUid))
                planetMaps.Add(mapUid);
        }

        return TryGetRandomTileOnGrids(planetMaps, out coordinates);
    }

    private bool TryGetRandomTileOnGrids(List<EntityUid> grids, out EntityCoordinates coordinates)
    {
        coordinates = default;

        if (grids.Count == 0)
            return false;

        for (var i = 0; i < 25; i++)
        {
            var grid = _random.Pick(grids);

            if (!TryComp<MapGridComponent>(grid, out var gridComp))
                continue;

            if (!TryPickRandomTile(grid, gridComp, out var tile))
                continue;

            var valid = true;
            foreach (var ent in _mapSystem.GetAnchoredEntities(grid, gridComp, tile))
            {
                if (!_physicsQuery.TryGetComponent(ent, out var body))
                    continue;
                if (body.BodyType != BodyType.Static ||
                    !body.Hard ||
                    (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                    continue;

                valid = false;
                break;
            }

            if (!valid)
                continue;

            coordinates = _mapSystem.GridTileToLocal(grid, gridComp, tile);
            return true;
        }

        return false;
    }

    private bool TryPickRandomTile(EntityUid grid, MapGridComponent gridComp, out Vector2i tile)
    {
        tile = default;
        var found = false;
        var seen = 0;

        foreach (var tileRef in _mapSystem.GetAllTiles(grid, gridComp))
        {
            seen++;
            if (_random.Next(seen) != 0)
                continue;

            tile = tileRef.GridIndices;
            found = true;
        }

        return found;
    }
}
