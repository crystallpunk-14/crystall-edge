using Content.Server._CE.MagicEssence.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Events;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server._CE.StationEvents;

/// <summary>
/// Generic station event: spawns <see cref="CETopTileSpawnRuleComponent.Prototype"/> at the first
/// free tile found scanning down from the topmost z-level of a random column in the station's
/// z-map network. Used e.g. to place a Lurker ghost role spawn point somewhere above the crew.
/// </summary>
public sealed partial class CETopTileSpawnRuleSystem : StationEventSystem<CETopTileSpawnRuleComponent>
{
    [Dependency] private CEMagicEssenceNodeSystem _essenceNode = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    private const int MaxAttempts = 25;

    protected override void Started(EntityUid ruleUid,
        CETopTileSpawnRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(ruleUid, component, gameRule, args);

        if (!_essenceNode.TryGetStationNetwork(out var network))
        {
            Log.Warning("CETopTileSpawnRuleSystem: couldn't resolve a station z-map network - nothing spawned.");
            return;
        }

        if (!TryGetTopFreeTile(network, out var coordinates))
        {
            Log.Warning("CETopTileSpawnRuleSystem: couldn't find a free tile - nothing spawned.");
            return;
        }

        Spawn(component.Prototype, coordinates);
    }

    /// <summary>
    /// Picks a random (x,y) column somewhere in the network, then walks it from the topmost
    /// z-level down, returning the first tile that both exists and isn't blocked by a static,
    /// hard, impassable anchored entity. Retries with a fresh column on failure.
    /// </summary>
    private bool TryGetTopFreeTile(Entity<CEZMapNetworkComponent> network, out EntityCoordinates coordinates)
    {
        coordinates = default;

        var planetMaps = new List<EntityUid>();
        foreach (var mapUid in network.Comp.SortedZLevels)
        {
            if (mapUid != EntityUid.Invalid && HasComp<MapGridComponent>(mapUid))
                planetMaps.Add(mapUid);
        }

        if (planetMaps.Count == 0)
            return false;

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var sourceGrid = _random.Pick(planetMaps);
            if (!TryComp<MapGridComponent>(sourceGrid, out var sourceGridComp) ||
                !TryPickRandomTile(sourceGrid, sourceGridComp, out var tile))
                continue;

            for (var depth = network.Comp.SortedMax; depth >= network.Comp.SortedMin; depth--)
            {
                var index = depth - network.Comp.SortedMin;
                if (index < 0 || index >= network.Comp.SortedZLevels.Count)
                    continue;

                var mapUid = network.Comp.SortedZLevels[index];
                if (mapUid == EntityUid.Invalid || !TryComp<MapGridComponent>(mapUid, out var gridComp))
                    continue;

                if (!IsTileFree(mapUid, gridComp, tile))
                    continue;

                coordinates = _mapSystem.GridTileToLocal(mapUid, gridComp, tile);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A tile is free if it actually exists (not space/void) and isn't blocked by a static, hard,
    /// impassable anchored entity - same check as <c>CEMagicEssenceNodeSystem</c> uses for node spawns.
    /// </summary>
    private bool IsTileFree(EntityUid grid, MapGridComponent gridComp, Vector2i tile)
    {
        if (!_mapSystem.TryGetTileRef(grid, gridComp, tile, out var tileRef) || tileRef.Tile.IsEmpty)
            return false;

        foreach (var ent in _mapSystem.GetAnchoredEntities(grid, gridComp, tile))
        {
            if (!_physicsQuery.TryGetComponent(ent, out var body))
                continue;

            if (body.BodyType == BodyType.Static && body.Hard && (body.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                return false;
        }

        return true;
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
