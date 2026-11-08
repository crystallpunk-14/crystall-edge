using Content.Server._CE.GameTicking;
using Content.Server._CE.GOAP;
using Content.Server._CE.ZLevels.Core.Components;
using Content.Server._CE.ZLevels.Weather;
using Content.Server.GameTicking.Rules;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Weather;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Weather;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.RitualSiege;

/// <summary>
/// Runs <see cref="CERitualSiegeComponent"/>: replaces the station weather with the murk storm once the
/// Restoration Ritual starts, then alternates calm and danger phases, spawning a player-scaled wave of
/// murk mobs around the Lucson Sphere at the start of every danger phase.
/// </summary>
public sealed partial class CERitualSiegeSystem : GameRuleSystem<CERitualSiegeComponent>
{
    [Dependency] private CEMurkConsumingRuleSystem _consuming = default!;
    [Dependency] private CEWeatherSystem _zWeather = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private CEGOAPSystem _goap = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    [SubscribeLocalEvent]
    private void OnSphereStateChanged(EntityUid uid, CEMurkLusconSphereComponent component, CEMurkSphereStateChangedEvent args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var siege, out _))
        {
            if (args.NewState == CEMurkSphereState.Ritual && siege.Sphere is null)
                StartSiege(siege, uid);
            else if (args.OldState == CEMurkSphereState.Ritual && siege.Sphere == uid)
                EndSiege(siege);
        }
    }

    /// <summary>
    /// Clears the storm and gives the weather back to the station once the ritual is over.
    /// </summary>
    private void EndSiege(CERitualSiegeComponent siege)
    {
        SetSiegeWeather(siege, null);

        if (siege.Network is { } network)
        {
            var query = EntityQueryEnumerator<CEStationWeatherComponent, CEStationZLevelsComponent>();
            while (query.MoveNext(out var stationWeather, out var zLevels))
            {
                if (zLevels.ZNetworkEntity != network)
                    continue;

                stationWeather.Enabled = true;
                stationWeather.CurrentNode = null; // Picks a fresh starting node right away.
            }
        }

        siege.Sphere = null;
        siege.Network = null;
        siege.Danger = false;
    }

    private void StartSiege(CERitualSiegeComponent siege, EntityUid sphere)
    {
        siege.Sphere = sphere;
        siege.Danger = false;
        siege.NextPhaseTime = Timing.CurTime + siege.FirstCalmDuration;

        if (Transform(sphere).MapUid is { } map)
            siege.Network = DisableStationWeather(map);

        SetSiegeWeather(siege, siege.CalmWeather);
    }

    /// <summary>
    /// Stops the automatic weather of the station whose z-network holds <paramref name="map"/> and returns that network.
    /// </summary>
    private EntityUid? DisableStationWeather(EntityUid map)
    {
        var query = EntityQueryEnumerator<CEStationWeatherComponent, CEStationZLevelsComponent>();
        while (query.MoveNext(out var stationWeather, out var zLevels))
        {
            if (zLevels.ZNetworkEntity is not { } network ||
                !TryComp<CEZMapNetworkComponent>(network, out var networkComp) ||
                !networkComp.ZLevels.ContainsValue(map))
                continue;

            stationWeather.Enabled = false;
            return network;
        }

        return null;
    }

    private void SetSiegeWeather(CERitualSiegeComponent siege, EntProtoId? weather)
    {
        if (siege.Network is { } network)
        {
            _zWeather.SetWeather(network, weather, null);
            return;
        }

        // No z-network: only the sphere's own map gets the storm.
        if (siege.Sphere is { } sphere && Exists(sphere))
            _weather.TrySetWeather(Transform(sphere).MapID, weather, out _, null);
    }

    protected override void ActiveTick(EntityUid uid, CERitualSiegeComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.Sphere is not { } sphere ||
            !TryComp<CEMurkLusconSphereComponent>(sphere, out var sphereComp) ||
            sphereComp.State != CEMurkSphereState.Ritual)
            return;

        if (Timing.CurTime < component.NextPhaseTime)
            return;

        component.Danger = !component.Danger;

        if (component.Danger)
        {
            component.NextPhaseTime = Timing.CurTime + RobustRandom.Next(component.MinDangerDuration, component.MaxDangerDuration);
            SetSiegeWeather(component, component.DangerWeather);
            SpawnWave(component, sphere);
        }
        else
        {
            component.NextPhaseTime = Timing.CurTime + RobustRandom.Next(component.MinCalmDuration, component.MaxCalmDuration);
            SetSiegeWeather(component, component.CalmWeather);
        }
    }

    private void SpawnWave(CERitualSiegeComponent siege, EntityUid sphere)
    {
        var sphereXform = Transform(sphere);
        if (sphereXform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
        {
            Log.Error($"{ToPrettyString(sphere)} is not on a grid - the ritual siege can't spawn its wave.");
            return;
        }

        _consuming.TryGetMaxOnline(out var maxOnline);
        _consuming.TryGetRitualProgress(out var progress);

        var multiplier = MathHelper.Lerp(siege.StartMultiplier, siege.EndMultiplier, progress);
        var threat = siege.BaseRolls + siege.RollsPerPlayer * MathF.Pow(maxOnline, siege.PlayerExponent);
        var rolls = (int) MathF.Round(threat * multiplier);

        var center = _transform.GetWorldPosition(sphereXform);
        for (var i = 0; i < rolls; i++)
        {
            foreach (var proto in _entityTable.GetSpawns(siege.Table, RobustRandom))
            {
                if (!TryFindSpawnTile(siege, (grid, gridComp), center, out var coords))
                    continue;

                var mob = Spawn(proto, coords);

                // The mobs see only a few tiles: tell them where the sphere is right away.
                if (TryComp<CEGOAPComponent>(mob, out var goap))
                    _goap.Remember((mob, goap), sphere, sphereXform.Coordinates, siege.SphereMemory);
            }
        }
    }

    /// <summary>
    /// Picks a random unblocked tile of the sphere's grid within the spawn ring around <paramref name="center"/>.
    /// </summary>
    private bool TryFindSpawnTile(
        CERitualSiegeComponent siege,
        Entity<MapGridComponent> grid,
        System.Numerics.Vector2 center,
        out EntityCoordinates coords)
    {
        for (var attempt = 0; attempt < siege.SpawnAttempts; attempt++)
        {
            var distance = RobustRandom.NextFloat(siege.MinSpawnRadius, siege.MaxSpawnRadius);
            var position = center + RobustRandom.NextAngle().ToVec() * distance;

            if (!_map.TryGetTileRef(grid, grid.Comp, position, out var tile) || tile.Tile.IsEmpty)
                continue;

            if (_turf.IsTileBlocked(grid, tile.GridIndices, CollisionGroup.Impassable))
                continue;

            coords = _map.GridTileToLocal(grid, grid.Comp, tile.GridIndices);
            return true;
        }

        coords = default;
        return false;
    }
}
