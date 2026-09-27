using System.Numerics;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Events;
using Content.Shared._CE.Murk;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server._CE.StationEvents;

/// <summary>
/// Generic station event: spawns <see cref="CEInLucsonSphereSpawnRuleComponent.Prototype"/> at a
/// random free tile within the (currently active) Lucson Sphere's murk radius, on any z-level that
/// radius reaches - see <see cref="CESharedMurkSystem.TryProjectSource"/>. Used e.g. to place a
/// hungry node inside the dead zone.
/// </summary>
public sealed partial class CEInLucsonSphereSpawnRuleSystem : StationEventSystem<CEInLucsonSphereSpawnRuleComponent>
{
    [Dependency] private CESharedMurkSystem _murk = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    private const int MaxTileAttempts = 25;

    protected override void Started(EntityUid ruleUid,
        CEInLucsonSphereSpawnRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(ruleUid, component, gameRule, args);

        if (!TryFindActiveSphere(out var sphere, out var sphereXform, out var intensity))
        {
            Log.Warning("CEInLucsonSphereSpawnRuleSystem: no active Lucson Sphere found - nothing spawned.");
            return;
        }

        if (sphereXform.MapUid is not { } sphereMapUid || !_zLevels.TryGetMapNetwork(sphereMapUid, out var network))
        {
            Log.Warning("CEInLucsonSphereSpawnRuleSystem: Lucson Sphere's map isn't part of a z-map network - nothing spawned.");
            return;
        }

        if (!TryGetPointInSphere(network, (sphere, sphereXform), intensity, out var coordinates))
        {
            Log.Warning("CEInLucsonSphereSpawnRuleSystem: couldn't find a free tile within the sphere's radius - nothing spawned.");
            return;
        }

        Spawn(component.Prototype, coordinates);
    }

    private bool TryFindActiveSphere(out EntityUid sphere, out TransformComponent sphereXform, out float intensity)
    {
        sphere = default;
        sphereXform = default!;
        intensity = 0f;

        var query = EntityQueryEnumerator<CEMurkLusconSphereComponent, CEMurkSourceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var source, out var xform))
        {
            if (!source.Active)
                continue;

            sphere = uid;
            sphereXform = xform;
            intensity = source.Intensity;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Shuffles every planet map in the sphere's z-network, then on the first one whose projected
    /// radius is non-zero, tries random tiles within that radius (existing, not blocked by a static
    /// hard impassable anchored entity) up to <see cref="MaxTileAttempts"/> times before moving to
    /// the next map.
    /// </summary>
    private bool TryGetPointInSphere(Entity<CEZMapNetworkComponent> network, Entity<TransformComponent> sphere, float intensity, out EntityCoordinates coordinates)
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

        _random.Shuffle(planetMaps);

        var sphereWorldPos = _transform.GetWorldPosition(sphere.Owner);

        foreach (var mapUid in planetMaps)
        {
            if (!_murk.TryProjectSource(mapUid, sphere, intensity, out var radius) || radius <= 0f)
                continue;

            if (!TryComp<MapGridComponent>(mapUid, out var gridComp))
                continue;

            for (var attempt = 0; attempt < MaxTileAttempts; attempt++)
            {
                if (!TryPickRandomTile(mapUid, gridComp, out var tile))
                    break;

                var tileWorldPos = _mapSystem.GridTileToWorldPos(mapUid, gridComp, tile);
                if (Vector2.Distance(sphereWorldPos, tileWorldPos) >= radius)
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
    /// impassable anchored entity - same check as <c>CEMagicEssenceNodeSystem</c>/<c>CETopTileSpawnRuleSystem</c>.
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
