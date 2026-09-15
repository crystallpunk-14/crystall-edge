using System.Numerics;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Map;

namespace Content.Shared._CE.Murk.Systems;

/// <summary>
/// Murk is a binary wall of darkness around the world: a map has a base intensity, and sources
/// (lighthouses) carve spheres out of it. This system owns the math - the client renders it and
/// the server asks it whether something stands in the murk, both from the same formula.
/// </summary>
public abstract partial class CESharedMurkSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    /// <summary>
    /// How many tiles of sphere radius one z-level costs.
    /// </summary>
    public float ZDistancePerLevel => CESharedZLevelsSystem.ZLevelOffset * _cfg.GetCVar(CCVars.CEMurkZScale);

    public float Threshold => _cfg.GetCVar(CCVars.CEMurkThreshold);

    /// <summary>
    /// Slices the source's sphere at <paramref name="zOffset"/> levels away from it. Returns false
    /// once the sphere no longer reaches that level.
    /// </summary>
    public bool TryProjectRadius(int zOffset, float intensity, out float radius)
    {
        radius = 0f;

        var full = MathF.Abs(intensity);
        if (full <= 0f)
            return false;

        if (zOffset == 0)
        {
            radius = full;
            return true;
        }

        var vertical = zOffset * ZDistancePerLevel;
        var squared = full * full - vertical * vertical;
        if (squared <= 0f)
            return false;

        radius = MathF.Sqrt(squared);
        return true;
    }

    /// <summary>
    /// Projects a source onto <paramref name="targetMap"/>, accounting for the z-distance between them.
    /// </summary>
    public bool TryProjectSource(EntityUid targetMap, Entity<TransformComponent> source, float intensity, out float radius)
    {
        radius = 0f;

        if (source.Comp.MapUid is not { } sourceMap)
            return false;

        if (!_zLevels.TryGetZLevelOffset(targetMap, sourceMap, out var zOffset))
            return false;

        return TryProjectRadius(zOffset, intensity, out radius);
    }

    /// <summary>
    /// Raw murk intensity at a world position, normalized to 0..1. No noise and no smoothing -
    /// this is the gameplay truth, the render only approximates it.
    /// </summary>
    public float GetMurkIntensity(EntityUid mapUid, Vector2 worldPos)
    {
        var total = 0f;
        if (TryComp<CEMurkedMapComponent>(mapUid, out var murkedMap))
            total = murkedMap.Intensity;

        var query = EntityQueryEnumerator<CEMurkSourceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var source, out var xform))
        {
            if (!source.Active)
                continue;

            if (!TryProjectSource(mapUid, (uid, xform), source.Intensity, out var radius))
                continue;

            var distance = Vector2.Distance(_transform.GetWorldPosition(uid), worldPos);
            if (distance >= radius)
                continue;

            total += source.Intensity * (1f - distance / radius);
        }

        return Math.Clamp(total, 0f, 1f);
    }

    public float GetMurkIntensity(EntityCoordinates coords)
    {
        if (_transform.GetMap(coords) is not { } mapUid)
            return 0f;

        return GetMurkIntensity(mapUid, _transform.ToMapCoordinates(coords).Position);
    }

    public bool InMurk(EntityCoordinates coords)
    {
        return GetMurkIntensity(coords) > Threshold;
    }

    public bool InMurk(EntityUid ent)
    {
        return InMurk(Transform(ent).Coordinates);
    }

    /// <summary>
    /// Ensures a <see cref="CEMurkedMapComponent"/> on every map of the given zNetwork and sets its intensity.
    /// </summary>
    public void SetNetworkIntensity(Entity<CEZMapNetworkComponent?> network, float intensity)
    {
        if (!Resolve(network, ref network.Comp))
            return;

        foreach (var (_, map) in network.Comp.ZLevels)
        {
            if (map is not { } mapUid)
                continue;

            var murkedMap = EnsureComp<CEMurkedMapComponent>(mapUid);
            murkedMap.Intensity = intensity;
            Dirty(mapUid, murkedMap);
        }
    }
}
