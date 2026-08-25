using Content.Server._CE.Weather.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._CE.Weather;

/// <summary>
/// Periodically pours weather-defined reagents (see <see cref="CEWeatherSolutionFillComponent"/>) into every
/// sky-exposed <see cref="CEWeatherRefillableComponent"/> (as long as it isn't closed via an
/// <see cref="OpenableComponent"/>).
/// </summary>
public sealed partial class CEWeatherSolutionFillSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    [Dependency] private EntityQuery<OpenableComponent> _openableQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<RoofComponent> _roofQuery = default!;

    /// <summary>
    /// Weather entities with a <see cref="CEWeatherSolutionFillComponent"/>, grouped by the map they're
    /// currently applied to. Rebuilt every tick.
    /// </summary>
    private readonly Dictionary<EntityUid, List<Entity<CEWeatherSolutionFillComponent>>> _activeFillWeathers = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var weathers in _activeFillWeathers.Values)
        {
            weathers.Clear();
        }

        var hasActiveWeather = false;

        var weatherQuery = EntityQueryEnumerator<WeatherStatusEffectComponent, CEWeatherSolutionFillComponent, StatusEffectComponent>();
        while (weatherQuery.MoveNext(out var weatherUid, out _, out var fill, out var status))
        {
            if (status.AppliedTo is not { } mapUid)
                continue;

            if (!_activeFillWeathers.TryGetValue(mapUid, out var weathers))
            {
                weathers = new List<Entity<CEWeatherSolutionFillComponent>>();
                _activeFillWeathers[mapUid] = weathers;
            }

            weathers.Add((weatherUid, fill));
            hasActiveWeather = true;
        }

        // No weather with solution fill anywhere - skip scanning refillable containers entirely.
        if (!hasActiveWeather)
            return;

        var refillableQuery = EntityQueryEnumerator<CEWeatherRefillableComponent, TransformComponent>();
        while (refillableQuery.MoveNext(out var uid, out var refillable, out var xform))
        {
            if (_timing.CurTime < refillable.NextFillTime)
                continue;

            if (xform.MapUid is not { } mapUid ||
                !_activeFillWeathers.TryGetValue(mapUid, out var weathers) ||
                weathers.Count == 0)
                continue;

            // Closed containers can't be filled; entities without OpenableComponent are always fillable.
            if (_openableQuery.TryGetComponent(uid, out var openable) && !openable.Opened)
                continue;

            if (xform.GridUid is not { } gridUid || !_gridQuery.TryGetComponent(gridUid, out var gridComp))
                continue;

            var tileRef = _mapSystem.GetTileRef((gridUid, gridComp), xform.Coordinates);
            _roofQuery.TryGetComponent(gridUid, out var roofComp);

            if (!_weather.CanWeatherAffect((gridUid, gridComp, roofComp), tileRef))
                continue;

            if (!_solutionContainer.ResolveSolution(uid, refillable.Solution, ref refillable.SolutionEntity))
                continue;

            var weather = weathers[0];
            foreach (var reagent in weather.Comp.Reagents)
            {
                _solutionContainer.TryAddReagent(refillable.SolutionEntity.Value, reagent, out _);
            }

            refillable.NextFillTime = _timing.CurTime + weather.Comp.Frequency;
        }
    }
}
