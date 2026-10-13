using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.GameTicking;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Storage.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Shared._CE.DayCycle;

/// <summary>
/// This is an add-on to the LightCycle system that helps you determine what time of day it is on the map
/// </summary>
public sealed partial class CEDayCycleSystem : EntitySystem
{
    private const float DefaultThreshold = 0.6f;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private SharedGameTicker _ticker = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedRoofSystem _roof = default!;

    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<InsideEntityStorageComponent> _storageQuery;

    public override void Initialize()
    {
        base.Initialize();

        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _storageQuery = GetEntityQuery<InsideEntityStorageComponent>();
    }

    [SubscribeLocalEvent]
    private void OnStartDay(Entity<CEZMapComponent> ent, ref CEStartDayEvent args)
    {
        if (ent.Comp.Depth == 0)
            RaiseLocalEvent(new CEGlobalStartDayEvent());
    }

    [SubscribeLocalEvent]
    private void OnStartNight(Entity<CEZMapComponent> ent, ref CEStartNightEvent args)
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
            var time = GetCycleTime(uid, lightCycle);

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

        return GetCurrentLightLevel(map) >= GetThreshold(map);
    }

    public float GetCurrentLightLevel(Entity<LightCycleComponent?> map)
    {
        if (!Resolve(map, ref map.Comp, false))
            return 0f;

        var time = GetCycleTime(map, map.Comp);

        return (float)SharedLightCycleSystem.CalculateLightLevel(map.Comp, time);
    }

    /// <summary>
    /// Returns how far the current day on the map has progressed: 0 is midnight, 0.5 is noon
    /// </summary>
    public bool TryGetDayProgress(Entity<LightCycleComponent?> map, out float progress)
    {
        progress = 0f;

        if (!Resolve(map, ref map.Comp, false))
            return false;

        var duration = GetCycleDuration(map.Comp);
        var time = GetCycleTime(map, map.Comp);

        progress = (time % duration + duration) % duration / duration;
        return true;
    }

    /// <summary>
    /// Returns how much time is left until the next dawn or sunset on the map.
    /// Returns false if the day threshold is never crossed there (it's always day or always night)
    /// </summary>
    public bool TryGetTimeUntilTransition(Entity<LightCycleComponent?> map, out TimeSpan remaining, out bool untilDawn)
    {
        remaining = TimeSpan.Zero;
        untilDawn = false;

        if (!Resolve(map, ref map.Comp, false) || !TryGetDayProgress(map, out var progress))
            return false;

        if (!TryGetDawnProgress(map, out var dawn))
            return false;

        var sunset = 1f - dawn;

        float left;
        if (progress >= dawn && progress < sunset)
        {
            left = sunset - progress;
        }
        else
        {
            untilDawn = true;
            left = dawn - progress;
            if (left < 0f)
                left += 1f;
        }

        remaining = TimeSpan.FromSeconds(MathF.Ceiling(left * GetCycleDuration(map.Comp)));
        return true;
    }

    /// <summary>
    /// Returns how far the daylight on the map has progressed: 0 at dawn, 1 at sunset.
    /// Returns false at night, or if the day threshold is never crossed there (it's always day or always night)
    /// </summary>
    public bool TryGetDaylightProgress(Entity<LightCycleComponent?> map, out float progress)
    {
        progress = 0f;

        if (!Resolve(map, ref map.Comp, false) || !IsDayNow(map))
            return false;

        if (!TryGetDayProgress(map, out var dayProgress) || !TryGetDawnProgress(map, out var dawn))
            return false;

        progress = Math.Clamp((dayProgress - dawn) / (1f - 2f * dawn), 0f, 1f);
        return true;
    }

    /// <summary>
    /// Cycle progress (0 is midnight, 0.5 is noon) at which the light level crosses the day threshold in the morning.
    /// Sunset is mirrored at 1 - dawn
    /// </summary>
    private bool TryGetDawnProgress(Entity<LightCycleComponent?> map, out float dawn)
    {
        dawn = 0f;

        if (!Resolve(map, ref map.Comp, false))
            return false;

        // Mirrors SharedLightCycleSystem.CalculateLightLevel: (crest - shift) * sin^6(PI * progress) + shift, clipped at ClipLight
        var crest = MathF.Max(0f, map.Comp.MaxLightLevel);
        var shift = MathF.Max(0f, map.Comp.MinLightLevel);
        var threshold = GetThreshold(map);

        if (threshold <= shift || threshold >= MathF.Min(crest, map.Comp.ClipLight))
            return false;

        dawn = MathF.Asin(MathF.Pow((threshold - shift) / (crest - shift), 1f / 6f)) / MathF.PI;
        return true;
    }

    /// <summary>
    /// Localized "time left until dawn/sunset" text for the map, see <see cref="TryGetTimeUntilTransition"/>
    /// </summary>
    public bool TryGetTimeUntilTransitionText(Entity<LightCycleComponent?> map, [NotNullWhen(true)] out string? text)
    {
        text = null;

        if (!TryGetTimeUntilTransition(map, out var remaining, out var untilDawn))
            return false;

        text = Loc.GetString(untilDawn ? "ce-day-cycle-until-dawn" : "ce-day-cycle-until-sunset", ("time", remaining));
        return true;
    }

    /// <summary>
    /// Localized "Day N" / "Night N" text for the map. Returns false until the first dawn of the round
    /// </summary>
    public bool TryGetDayNumberText(Entity<CEDayCycleComponent?> map, [NotNullWhen(true)] out string? text)
    {
        text = null;

        if (!Resolve(map, ref map.Comp, false) || map.Comp.DayNumber <= 0)
            return false;

        text = Loc.GetString(IsDayNow(map.Owner) ? "ce-day-cycle-day" : "ce-day-cycle-night", ("number", map.Comp.DayNumber));
        return true;
    }

    /// <summary>
    /// Text shown by clocks: the current day number and the time left until dawn or sunset
    /// </summary>
    public bool TryGetClockText(EntityUid map, [NotNullWhen(true)] out string? text)
    {
        if (!TryGetTimeUntilTransitionText(map, out text))
            return false;

        if (TryGetDayNumberText(map, out var day))
            text = $"{day}\n{text}";

        return true;
    }

    private float GetCycleTime(EntityUid map, LightCycleComponent lightCycle)
    {
        return (float) _timing.CurTime
            .Add(lightCycle.Offset)
            .Subtract(_ticker.RoundStartTimeSpan)
            .Subtract(_meta.GetPauseTime(map))
            .TotalSeconds;
    }

    /// <summary>
    /// Same wave length as SharedLightCycleSystem.CalculateLightLevel uses
    /// </summary>
    private static float GetCycleDuration(LightCycleComponent lightCycle)
    {
        return MathF.Max(1f, (float) lightCycle.Duration.TotalSeconds);
    }

    private float GetThreshold(EntityUid map)
    {
        return TryComp<CEDayCycleComponent>(map, out var dayCycle) ? dayCycle.Threshold : DefaultThreshold;
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

        if (!TryComp<RoofComponent>(grid.Value, out var roofComp))
            return day;

        // Check if the tile is illuminated (not under a roof)
        var tileRef = _maps.GetTileRef(xform.GridUid.Value, gridComp, xform.Coordinates);
        if (_roof.IsRooved((grid.Value, gridComp, roofComp), tileRef.GridIndices))
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
