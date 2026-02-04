using Content.Shared.Random.Rules;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;

namespace Content.Shared._CE.DayCycle;

/// <summary>
/// Checks whether there is a time of day on the current map, and whether the current time of day corresponds to the specified periods.
/// </summary>
public sealed partial class CEIsNight : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        var transform = entManager.System<SharedTransformSystem>();
        var dayCycle = entManager.System<CEDayCycleSystem>();

        var map = transform.GetMap(uid);

        if (map is null)
            return false;

        var isDay = dayCycle.IsDayNow(map.Value);

        return Inverted ? isDay : !isDay;
    }
}


public sealed partial class CEWeatherAffect : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        var transform = entManager.System<SharedTransformSystem>();
        var weather = entManager.System<SharedWeatherSystem>();
        var mapSystem = entManager.System<SharedMapSystem>();

        var gridUid = transform.GetGrid(uid);
        if (gridUid is null)
            return false;

        if (!entManager.TryGetComponent<MapGridComponent>(gridUid.Value, out var grid))
            return false;

        var coordinates = transform.GetMapCoordinates(uid);
        var tileRef = mapSystem.GetTileRef(gridUid.Value, grid, coordinates);

        var weatherAffect = weather.CanWeatherAffect(gridUid.Value, grid, tileRef);

        return Inverted ? !weatherAffect : weatherAffect;
    }
}
