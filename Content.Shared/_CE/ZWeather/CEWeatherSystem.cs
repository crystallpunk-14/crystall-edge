using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.EntitySystems;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;

namespace Content.Shared._CE.ZWeather;

/// <summary>
/// A subsystem that connects WeatherSystem with ZLevelSystem. Allows you to control the weather for the entire z-network at once.
/// </summary>
public sealed class CEWeatherSystem : EntitySystem
{
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly CESharedZLevelsSystem _zLevel = default!;

    public void SetWeather(Entity<CEZLevelsNetworkComponent?> network, WeatherPrototype? proto, TimeSpan? endTime)
    {
        if (!Resolve(network, ref network.Comp))
            return;

        foreach (var (depth, map) in network.Comp.ZLevels)
        {
            if (!TryComp<MapComponent>(map, out var mapComp))
                continue;

            _weather.SetWeather(mapComp.MapId, proto, endTime);
        }
    }
}
