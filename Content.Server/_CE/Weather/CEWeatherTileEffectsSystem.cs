using Content.Server._CE.Weather.Components;
using Content.Shared.Light.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Weather;

/// <summary>
/// Periodically picks a single random weather-exposed tile under active weathers and raises
/// <see cref="CEWeatherTileAffectedEvent"/> for it — e.g. for random lightning strikes.
/// Unlike <see cref="CEWeatherEffectsSystem"/>, this doesn't scan for entities: it uses reservoir
/// sampling over exposed tiles in a single pass, since only one tile is picked per cycle.
/// </summary>
public sealed partial class CEWeatherTileEffectsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    [Dependency] private EntityQuery<RoofComponent> _roofQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEWeatherTileEffectsComponent, ComponentInit>(OnTileEffectsInit);
    }

    private void OnTileEffectsInit(Entity<CEWeatherTileEffectsComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextEffectTime = _timing.CurTime + _random.Next(ent.Comp.MinEffectFrequency, ent.Comp.MaxEffectFrequency);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WeatherStatusEffectComponent, CEWeatherTileEffectsComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out _, out var tileEffects, out var status))
        {
            if (_timing.CurTime < tileEffects.NextEffectTime)
                continue;

            var freq = _random.Next(tileEffects.MinEffectFrequency, tileEffects.MaxEffectFrequency);
            tileEffects.NextEffectTime += freq;

            if (status.AppliedTo is not { } mapUid || !TryComp<MapComponent>(mapUid, out var mapComp))
                continue;

            if (TryPickRandomExposedTile(mapComp.MapId, out var coords))
            {
                var ev = new CEWeatherTileAffectedEvent(coords);
                RaiseLocalEvent(uid, ref ev);
            }
        }
    }

    /// <summary>
    /// Reservoir-samples a single weather-exposed tile across all grids on the map in one pass.
    /// </summary>
    private bool TryPickRandomExposedTile(MapId mapId, out EntityCoordinates coords)
    {
        var found = 0;
        var bestGrid = EntityUid.Invalid;
        var bestIndices = default(Vector2i);

        foreach (var grid in _mapManager.GetAllGrids(mapId))
        {
            var gridUid = grid.Owner;
            var gridComp = grid.Comp;
            _roofQuery.TryGetComponent(gridUid, out var roofComp);

            var enumerator = _mapSystem.GetAllTilesEnumerator(gridUid, gridComp);
            while (enumerator.MoveNext(out var tileRef))
            {
                if (!_weather.CanWeatherAffect((gridUid, gridComp, roofComp), tileRef.Value))
                    continue;

                found++;
                if (_random.Prob(1f / found))
                {
                    bestGrid = gridUid;
                    bestIndices = tileRef.Value.GridIndices;
                }
            }
        }

        if (found == 0)
        {
            coords = default;
            return false;
        }

        coords = _mapSystem.GridTileToLocal(bestGrid, Comp<MapGridComponent>(bestGrid), bestIndices);
        return true;
    }
}

/// <summary>
/// Raised on the weather entity when a random exposed tile is struck.
/// Subscribe on <see cref="Content.Shared._CE.Weather.CEWeatherTileEntityEffectComponent"/> to handle it.
/// </summary>
[ByRefEvent]
public record struct CEWeatherTileAffectedEvent(EntityCoordinates Coordinates);
