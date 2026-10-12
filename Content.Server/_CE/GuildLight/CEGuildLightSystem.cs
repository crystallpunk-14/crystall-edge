using Content.Server.Power.EntitySystems;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.GuildLight;
using Content.Shared._CE.Murk;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.GuildLight;

/// <summary>
/// Creates the guild light on a random powered altar at dawn and dims it through the day.
/// </summary>
public sealed partial class CEGuildLightSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private CEDayCycleSystem _dayCycle = default!;
    [Dependency] private CESharedMurkSystem _murk = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEGuildLightComponent> ent, ref MapInitEvent args)
    {
        // Lights spawned outside of dawn (admins, mapping) live until the nearest sunset of their map
        ent.Comp.DawnTime = _timing.CurTime;
        ent.Comp.SunsetTime = _timing.CurTime;

        if (_transform.GetMap(ent.Owner) is { } map
            && _dayCycle.TryGetTimeUntilTransition(map, out var remaining, out var untilDawn)
            && !untilDawn)
        {
            ent.Comp.SunsetTime += remaining;
        }
    }

    [SubscribeLocalEvent]
    private void OnStartDay(CEStartDayEvent args)
    {
        if (!TryComp<CEZMapComponent>(args.MapUid, out var zMap) || zMap.Depth != 0)
            return;

        if (!_dayCycle.TryGetDayDuration(args.MapUid, out var dayDuration))
            return;

        var lights = EntityQueryEnumerator<CEGuildLightComponent>();
        while (lights.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }

        var altars = new List<Entity<CEGuildLightAltarComponent>>();
        var query = EntityQueryEnumerator<CEGuildLightAltarComponent>();
        while (query.MoveNext(out var uid, out var altar))
        {
            if (_power.IsPowered(uid))
                altars.Add((uid, altar));
        }

        if (altars.Count == 0)
            return;

        var chosen = _random.Pick(altars);
        var light = SpawnAtPosition(chosen.Comp.Light, Transform(chosen).Coordinates);
        if (!TryComp<CEGuildLightComponent>(light, out var lightComp))
            return;

        lightComp.DawnTime = _timing.CurTime;
        lightComp.SunsetTime = _timing.CurTime + dayDuration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CEGuildLightComponent, CEMurkSourceComponent>();
        while (query.MoveNext(out var uid, out var light, out var source))
        {
            var charge = GetCharge(light, now);
            if (charge <= 0f)
            {
                QueueDel(uid);
                continue;
            }

            var intensity = light.MaxIntensity * charge;
            if (MathF.Abs(intensity - source.Intensity) < light.SyncThreshold)
                continue;

            _murk.SetSourceIntensity((uid, source), intensity);
            _pointLight.SetRadius(uid, MathF.Abs(intensity));
        }
    }

    /// <summary>
    /// Share of <see cref="CEGuildLightComponent.MaxIntensity"/> the light has at <paramref name="now"/>.
    /// </summary>
    private static float GetCharge(CEGuildLightComponent light, TimeSpan now)
    {
        if (now < light.SunsetTime)
        {
            var day = (light.SunsetTime - light.DawnTime).TotalSeconds;
            var progress = day > 0 ? (float) ((now - light.DawnTime).TotalSeconds / day) : 1f;
            return 1f - (1f - light.DuskFraction) * Math.Clamp(progress, 0f, 1f);
        }

        var collapse = (float) ((now - light.SunsetTime).TotalSeconds / light.CollapseDuration.TotalSeconds);
        return light.DuskFraction * (1f - collapse);
    }
}
