using System.Numerics;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Power;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Murk.Systems;

public abstract partial class CESharedMurkSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CEMurkGeneratorComponent> ent, ref ChargeChangedEvent args)
    {
        if (!TryComp<CEMurkSourceComponent>(ent, out var source))
            return;

        source.Intensity = MathHelper.Lerp(ent.Comp.DisabledIntensity, ent.Comp.EnabledIntensity, args.CurrentCharge / args.MaxCharge);
        Dirty(ent, source);
    }

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
