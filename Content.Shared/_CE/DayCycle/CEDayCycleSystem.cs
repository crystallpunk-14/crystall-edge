using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.GameTicking;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Shared._CE.DayCycle;

/// <summary>
/// This is an add-on to the LightCycle system that helps you determine what time of day it is on the map
/// </summary>
public sealed class CEDayCycleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedGameTicker _ticker = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;

    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<InsideEntityStorageComponent> _storageQuery;

    public override void Initialize()
    {
        base.Initialize();

        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _storageQuery = GetEntityQuery<InsideEntityStorageComponent>();

        SubscribeLocalEvent<CEZLevelMapComponent, CEStartDayEvent>(OnStartDay);
        SubscribeLocalEvent<CEZLevelMapComponent, CEStartNightEvent>(OnStartNight);
    }

    private void OnStartDay(Entity<CEZLevelMapComponent> ent, ref CEStartDayEvent args)
    {
        if (ent.Comp.Depth == 0)
            RaiseLocalEvent(new CEGlobalStartDayEvent());
    }

    private void OnStartNight(Entity<CEZLevelMapComponent> ent, ref CEStartNightEvent args)
    {
        if (ent.Comp.Depth == 0)
            RaiseLocalEvent(new CEGlobalStartNightEvent());
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LightCycleComponent, CEDayCycleComponent, MapComponent>();
        while (query.MoveNext(out var uid, out var lightCycle, out var dayCycle, out var map))
        {
            var time = (float) _timing.CurTime
                .Add(lightCycle.Offset)
                .Subtract(_ticker.RoundStartTimeSpan)
                .Subtract(_meta.GetPauseTime(uid))
                .TotalSeconds;

            var oldLightLevel = dayCycle.LastLightLevel;
            var newLightLevel = (float)SharedLightCycleSystem.CalculateLightLevel(lightCycle, time);

            // Going into darkness
            if (oldLightLevel > newLightLevel)
            {
                if (oldLightLevel > dayCycle.Threshold)
                {
                    if (newLightLevel < dayCycle.Threshold)
                    {
                        var ev = new CEStartNightEvent(uid);
                        RaiseLocalEvent(uid, ev, true);
                    }
                }
            }

            // Going into light
            if (oldLightLevel < newLightLevel)
            {
                if (oldLightLevel < dayCycle.Threshold)
                {
                    if (newLightLevel > dayCycle.Threshold)
                    {
                        var ev = new CEStartDayEvent(uid);
                        RaiseLocalEvent(uid, ev, true);
                    }
                }
            }

            dayCycle.LastLightLevel = newLightLevel;
        }
    }

    public bool IsDayNow(Entity<LightCycleComponent?> map)
    {
        if (!Resolve(map, ref map.Comp, false))
            return false;

        return GetCurrentLightLevel(map) >= 0.4;
    }

    public float GetCurrentLightLevel(Entity<LightCycleComponent?> map)
    {
        if (!Resolve(map, ref map.Comp, false))
            return 0f;

        var time = (float) _timing.CurTime
            .Add( map.Comp.Offset)
            .Subtract(_ticker.RoundStartTimeSpan)
            .Subtract(_meta.GetPauseTime(map))
            .TotalSeconds;

        return (float)SharedLightCycleSystem.CalculateLightLevel(map.Comp, time);
    }

    /// <summary>
    /// Checks to see if the specified entity is on the map where it's daytime, and under the open sky
    /// </summary>
    public bool UnderSunlight(EntityUid target)
    {
        if (_storageQuery.HasComp(target))
            return false;

        var xform = Transform(target);

        if (xform.MapUid is null || xform.GridUid is null)
            return false;

        var day = IsDayNow(xform.MapUid.Value);

        var grid = xform.GridUid;
        if (grid is null)
            return day;

        if (!_mapGridQuery.TryComp(grid, out var gridComp))
            return day;

        if (!_weather.CanWeatherAffect(grid.Value,
                gridComp,
                _maps.GetTileRef(xform.GridUid.Value, gridComp, xform.Coordinates)))
            return false;

        return day;
    }
}


/// <summary>
/// Called on the map with <see cref="LightCycleComponent"/> when day ends and night begins
/// </summary>
public sealed class CEStartNightEvent(EntityUid mapUid) : EntityEventArgs
{
    public EntityUid MapUid = mapUid;
}

/// <summary>
/// Called on the map with <see cref="LightCycleComponent"/> when night ends and dawn begins
/// </summary>
public sealed class CEStartDayEvent(EntityUid mapUid) : EntityEventArgs
{
    public EntityUid MapUid = mapUid;
}

/// <summary>
/// called as bloadcast when the day begins on the main station map
/// </summary>
public sealed class CEGlobalStartDayEvent : EntityEventArgs
{
}

/// <summary>
/// called as bloadcast when the night begins on the main station map
/// </summary>
public sealed class CEGlobalStartNightEvent : EntityEventArgs
{
}
