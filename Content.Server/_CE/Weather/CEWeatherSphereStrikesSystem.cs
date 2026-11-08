using System.Diagnostics.CodeAnalysis;
using Content.Server._CE.Weather.Components;
using Content.Shared._CE.Murk.Components;
using Content.Shared.Light.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Weather;

/// <summary>
/// Strikes random open-sky tiles around the Lucson Sphere for weathers with <see cref="CEWeatherSphereStrikesComponent"/>.
/// Samples random points of the circle instead of scanning the map, since the circle is a small part of it.
/// </summary>
public sealed partial class CEWeatherSphereStrikesSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<RoofComponent> _roofQuery = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<CEWeatherSphereStrikesComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextStrikeTime = _timing.CurTime + _random.Next(ent.Comp.MinFrequency, ent.Comp.MaxFrequency);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WeatherStatusEffectComponent, CEWeatherSphereStrikesComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out _, out var strikes, out var status))
        {
            if (_timing.CurTime < strikes.NextStrikeTime)
                continue;

            strikes.NextStrikeTime += _random.Next(strikes.MinFrequency, strikes.MaxFrequency);

            if (status.AppliedTo is not { } map || !TryFindSphere(map, out var sphere))
                continue;

            if (!TryPickTile(sphere.Value, strikes, out var coords))
                continue;

            var ev = new CEWeatherTileAffectedEvent(coords);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private bool TryFindSphere(EntityUid map, [NotNullWhen(true)] out EntityUid? sphere)
    {
        var query = EntityQueryEnumerator<CEMurkLusconSphereComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid != map)
                continue;

            sphere = uid;
            return true;
        }

        sphere = null;
        return false;
    }

    private bool TryPickTile(EntityUid sphere, CEWeatherSphereStrikesComponent strikes, out EntityCoordinates coords)
    {
        coords = default;

        var xform = Transform(sphere);
        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        _roofQuery.TryGetComponent(grid, out var roof);
        var center = _transform.GetWorldPosition(xform);

        for (var attempt = 0; attempt < strikes.Attempts; attempt++)
        {
            // Square root keeps the strikes uniform over the circle's area instead of crowding its center.
            var distance = strikes.Radius * MathF.Sqrt(_random.NextFloat());
            var position = center + _random.NextAngle().ToVec() * distance;

            if (!_map.TryGetTileRef(grid, gridComp, position, out var tile))
                continue;

            if (!_weather.CanWeatherAffect((grid, gridComp, roof), tile))
                continue;

            coords = _map.GridTileToLocal(grid, gridComp, tile.GridIndices);
            return true;
        }

        return false;
    }
}
