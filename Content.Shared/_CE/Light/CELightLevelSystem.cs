using System.Numerics;
using Content.Shared._CE.DayCycle;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Containers;
using Robust.Shared.Light;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;

namespace Content.Shared._CE.Light;

/// <summary>
/// Gameplay light level at a point: engine point lights plus the day-cycle sky.
/// The sky is blocked by roofs, and everything is blocked by light-occluding containers.
/// </summary>
/// <remarks>
/// The engine <see cref="LightLevelSystem"/> adds the map's <see cref="MapLightComponent.AmbientLightColor"/>
/// everywhere, but the day cycle only updates that color on the client, and the engine knows nothing about roofs.
/// This system swaps the ambient term for the current sky color computed on demand, so it gives the same answer
/// on server and client without a server-side update loop.
/// Point lights on the server need the <c>lookup.enable_server_light_tree</c> cvar; without it only the sky is counted.
/// </remarks>
public sealed partial class CELightLevelSystem : EntitySystem
{
    [Dependency] private LightLevelSystem _lightLevel = default!;
    [Dependency] private SharedLightTreeSystem _lightTree = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedRoofSystem _roof = default!;
    [Dependency] private CEDayCycleSystem _dayCycle = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<MapLightComponent> _mapLightQuery = default!;
    [Dependency] private EntityQuery<LightCycleComponent> _lightCycleQuery = default!;
    [Dependency] private EntityQuery<CELightCycleComponent> _ceLightCycleQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<RoofComponent> _roofQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        if (_net.IsServer && !_lightTree.IsAvailable)
        {
            Log.Warning("Server light tree is disabled (lookup.enable_server_light_tree = false): " +
                        "gameplay light levels will only count the sky, not point lights.");
        }
    }

    /// <summary>
    /// Light level at the entity's position, from 0 (dark) to 1 (fully lit).
    /// Returns 0 if the entity is inside a container that occludes light.
    /// </summary>
    public bool TryGetLightLevel(EntityUid uid, out float level)
    {
        level = 0f;

        if (InsideOccludingContainer(uid))
            return true;

        return TryGetLightLevel(Transform(uid).Coordinates, out level);
    }

    /// <summary>
    /// Light level at the given coordinates, from 0 (dark) to 1 (fully lit).
    /// </summary>
    public bool TryGetLightLevel(EntityCoordinates coordinates, out float level)
    {
        level = 0f;

        if (!TryGetLightColor(coordinates, out var color))
            return false;

        level = _lightLevel.ColorToLevel(color);
        return true;
    }

    /// <summary>
    /// Additive light color at the given coordinates. See <see cref="CELightLevelSystem"/>.
    /// </summary>
    public bool TryGetLightColor(EntityCoordinates coordinates, out Color color)
    {
        color = Color.Black;

        var mapCoords = _transform.ToMapCoordinates(coordinates);
        if (!_map.TryGetMap(mapCoords.MapId, out var mapUid) || !_mapQuery.TryComp(mapUid.Value, out var map))
            return false;

        // The renderer skips lighting on such maps, everything is fully lit
        if (!map.LightingEnabled)
        {
            color = Color.White;
            return true;
        }

        var staticAmbient = _mapLightQuery.CompOrNull(mapUid.Value)?.AmbientLightColor ?? MapLightComponent.DefaultColor;

        // Point lights only: the engine result always includes the map's ambient color, which may be stale here
        var lights = Color.Black;
        if (_lightTree.IsAvailable && _lightLevel.TryCalculateLightColor(mapCoords, out var engineColor))
            lights = new Color(Vector4.Max(engineColor.RGBA - staticAmbient.RGBA, Vector4.Zero));

        var sky = IsRoofed(coordinates)
            ? Color.Black
            : GetSkyColor(mapUid.Value, staticAmbient);

        color = new Color(lights.RGBA + sky.RGBA);
        return true;
    }

    /// <summary>
    /// Current ambient sky color of the map, as the client renders it
    /// </summary>
    private Color GetSkyColor(EntityUid mapUid, Color staticAmbient)
    {
        if (!_lightCycleQuery.TryComp(mapUid, out var cycle))
            return staticAmbient;

        var time = _dayCycle.GetCycleTime(mapUid, cycle);

        if (_ceLightCycleQuery.TryComp(mapUid, out var ceCycle))
            return CESharedLightCycleSystem.GetColor(ceCycle, cycle.Duration, time);

        return SharedLightCycleSystem.GetColor((mapUid, cycle), cycle.OriginalColor, time);
    }

    private bool IsRoofed(EntityCoordinates coordinates)
    {
        if (_transform.GetGrid(coordinates) is not { } gridUid ||
            !_gridQuery.TryComp(gridUid, out var grid) ||
            !_roofQuery.TryComp(gridUid, out var roof))
            return false;

        var indices = _map.TileIndicesFor(gridUid, grid, coordinates);
        return _roof.IsRooved((gridUid, grid, roof), indices);
    }

    private bool InsideOccludingContainer(EntityUid uid)
    {
        var current = uid;
        while (_container.TryGetContainingContainer((current, null, null), out var container))
        {
            if (container.OccludesLight)
                return true;

            current = container.Owner;
        }

        return false;
    }
}
