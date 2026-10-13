using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Murk;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.MurkShield;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.MurkShield;

/// <summary>
/// Creates the murk shield on a random powered generator at dawn and dims it with the time of day.
/// </summary>
public sealed partial class CEMurkShieldSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private CEDayCycleSystem _dayCycle = default!;
    [Dependency] private CESharedMurkSystem _murk = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnStartDay(CEStartDayEvent args)
    {
        if (!TryComp<CEZMapComponent>(args.MapUid, out var zMap) || zMap.Depth != 0)
            return;

        var shields = EntityQueryEnumerator<CEMurkShieldComponent>();
        while (shields.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }

        var generators = new List<Entity<CEMurkShieldGeneratorComponent>>();
        var powered = new List<Entity<CEMurkShieldGeneratorComponent>>();
        var query = EntityQueryEnumerator<CEMurkShieldGeneratorComponent>();
        while (query.MoveNext(out var uid, out var generator))
        {
            generators.Add((uid, generator));

            if (_power.IsPowered(uid))
                powered.Add((uid, generator));
        }

        if (powered.Count == 0)
            return;

        var chosen = _random.Pick(powered);
        SpawnAtPosition(chosen.Comp.Shield, Transform(chosen).Coordinates);

        foreach (var generator in generators)
        {
            if (generator.Owner == chosen.Owner)
                continue;

            SpawnAtPosition(generator.Comp.SpawnFailureEffect, Transform(generator).Coordinates);
            _popup.PopupEntity(Loc.GetString(generator.Comp.SpawnFailurePopup), generator, PopupType.LargeCaution);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEMurkShieldComponent, CEMurkSourceComponent>();
        while (query.MoveNext(out var uid, out var shield, out var source))
        {
            var charge = GetCharge((uid, shield));
            if (charge <= 0f)
            {
                QueueDel(uid);
                continue;
            }

            var intensity = shield.MaxIntensity * charge;
            if (MathF.Abs(intensity - source.Intensity) < shield.SyncThreshold)
                continue;

            _murk.SetSourceIntensity((uid, source), intensity);
            _pointLight.SetRadius(uid, MathF.Abs(intensity));
        }
    }

    /// <summary>
    /// Share of <see cref="CEMurkShieldComponent.MaxIntensity"/> the shield has right now.
    /// Starts the collapse the first time the shield sees the night.
    /// </summary>
    private float GetCharge(Entity<CEMurkShieldComponent> ent)
    {
        if (ent.Comp.CollapseStart is null)
        {
            // Maps without a day cycle never run the shield down
            if (GetSurfaceMap(ent) is not { } surface || !HasComp<LightCycleComponent>(surface))
                return 1f;

            if (_dayCycle.TryGetDaylightProgress(surface, out var progress))
                return 1f - (1f - ent.Comp.DuskFraction) * progress;

            ent.Comp.CollapseStart = _timing.CurTime;
        }

        var collapse = (_timing.CurTime - ent.Comp.CollapseStart.Value) / ent.Comp.CollapseDuration;
        return ent.Comp.DuskFraction * (1f - (float) collapse);
    }

    /// <summary>
    /// The day cycle lives on the surface of the z-network, underground levels follow it.
    /// </summary>
    private EntityUid? GetSurfaceMap(EntityUid uid)
    {
        if (_transform.GetMap(uid) is not { } map)
            return null;

        if (_zLevels.TryGetMapNetwork(map, out var network)
            && network.Comp.ZLevels.TryGetValue(0, out var surface)
            && surface is not null)
        {
            return surface;
        }

        return map;
    }
}
