using System.Numerics;
using Content.Shared._CE.Murk.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Murk.Systems;

public abstract partial class CESharedMurkSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public bool InMurk(EntityCoordinates coords)
    {
        var totalIntensity = 0f;
        if (TryComp<CEMurkedMapComponent>(_transform.GetMap(coords), out var murkedMap))
            totalIntensity = murkedMap.Intensity;

        var mapId = _transform.GetMapId(coords);

        var query = EntityQueryEnumerator<CEMurkSourceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var source, out var xform))
        {
            if (!source.Active || xform.MapID != mapId)
                continue;

            var distance = Vector2.Distance(_transform.GetWorldPosition(uid), coords.Position);

            if (distance <= MathF.Abs(source.Intensity))
                totalIntensity += source.Intensity * (1 - distance / MathF.Abs(source.Intensity));

        }

        return totalIntensity > 0.5f;
    }

    public bool InMurk(EntityUid ent)
    {
        return InMurk(Transform(ent).Coordinates);
    }
}
