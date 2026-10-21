/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using Content.Server._CE.ZLevels.Core;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Roof;
using Content.Shared.Light.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.ZLevels.Roof;

public sealed partial class CEZLevelsRoofSystem
{
    [SubscribeLocalEvent]
    private void OnMapNetworkUpdated(CEZLevelMapNetworkUpdatedEvent args)
    {
        if (!TryComp<CEZMapNetworkComponent>(args.Network, out var network))
            return;

        RecalculateMapRoofs((args.Network, network));
    }

    /// <summary>
    /// The marker can be (re)created after the network was built, e.g. when z-network components are added
    /// on MapInit of a map that was uninitialized at network creation, wiping the weather mask.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnZLevelRoofStartup(Entity<CEZLevelRoofComponent> ent, ref ComponentStartup args)
    {
        if (!ZMapQuery.HasComp(ent) || !ZLevel.TryGetMapNetwork(ent.Owner, out var network))
            return;

        RecalculateMapRoofs(network);
    }

    /// <summary>
    /// Planetary map path: propagate roof and weather state down through the z-map network.
    /// </summary>
    private void OnMapTileChanged(
        EntityUid mapUid,
        MapGridComponent currentMapGrid,
        RoofComponent currentRoof,
        CEZMapComponent zLevelMapComp,
        TileChangedEvent args)
    {
        var mapsBelow = ZLevel.GetAllMapsBelow((mapUid, zLevelMapComp));
        if (mapsBelow.Count == 0)
            return;

        ZRoofQuery.TryComp(mapUid, out var currentZRoof);

        var covers = new (Vector2i Indices, bool Shadow, bool Weather)[args.Changes.Length];
        for (var i = 0; i < args.Changes.Length; i++)
        {
            var change = args.Changes[i];
            var shadow = Roof.IsRooved((mapUid, currentMapGrid, currentRoof), change.GridIndices) ||
                         CastsShadow(change.NewTile);
            var weather = IsWeatherBlocked((mapUid, currentZRoof), change.GridIndices) ||
                          BlocksWeather(change.NewTile);
            covers[i] = (change.GridIndices, shadow, weather);
        }

        // Nearest first: each level's own tile re-shields everything further down.
        foreach (var mapBelow in mapsBelow)
        {
            if (!GridQuery.TryComp(mapBelow, out var mapGridBelow))
                continue;

            var roofBelow = EnsureComp<RoofComponent>(mapBelow);
            ZRoofQuery.TryComp(mapBelow, out var zRoofBelow);

            for (var i = 0; i < covers.Length; i++)
            {
                ref var cover = ref covers[i];

                Roof.SetRoof((mapBelow, mapGridBelow, roofBelow), cover.Indices, cover.Shadow);
                if (zRoofBelow is not null)
                    SetWeatherBlocked((mapBelow, zRoofBelow), cover.Indices, cover.Weather);

                if (!Map.TryGetTile(mapGridBelow, cover.Indices, out var tile))
                    continue;

                cover.Shadow |= CastsShadow(tile);
                cover.Weather |= BlocksWeather(tile);
            }
        }
    }

    private void RecalculateMapRoofs(Entity<CEZMapNetworkComponent> network)
    {
        _roofMap.Clear();
        _weatherMap.Clear();

        List<EntityUid> sortedMaps = new();
        foreach (var mapUid in network.Comp.ZLevels
                     .OrderByDescending(kv => kv.Key) // depth sorting
                     .Select(kv => kv.Value)
                     .Where(uid => uid.HasValue)
                     .Select(uid => uid!.Value))
        {
            sortedMaps.Add(mapUid);
        }

        foreach (var map in sortedMaps)
        {
            if (!GridQuery.TryComp(map, out var mapGrid))
                continue;

            // Everything accumulated from the levels above covers this one, including its empty tiles
            // (e.g. an atrium hole under a glass roof).
            var roofComp = EnsureComp<RoofComponent>(map);
            roofComp.Data.Clear();
            Dirty(map, roofComp);
            foreach (var indices in _roofMap)
            {
                Roof.SetRoof((map, mapGrid, roofComp), indices, true);
            }

            if (ZRoofQuery.TryComp(map, out var zRoof))
            {
                zRoof.WeatherData.Clear();
                Dirty(map, zRoof);
                foreach (var indices in _weatherMap)
                {
                    SetWeatherBlocked((map, zRoof), indices, true);
                }
            }

            var enumerator = Map.GetAllTilesEnumerator(map, mapGrid);
            while (enumerator.MoveNext(out var tileRef))
            {
                if (CastsShadow(tileRef.Value.Tile))
                    _roofMap.Add(tileRef.Value.GridIndices);

                if (BlocksWeather(tileRef.Value.Tile))
                    _weatherMap.Add(tileRef.Value.GridIndices);
            }
        }
    }
}
