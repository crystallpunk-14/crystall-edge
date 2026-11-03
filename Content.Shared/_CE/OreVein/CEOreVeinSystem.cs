using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityTable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._CE.OreVein;

/// <summary>
/// System that manages ore veins, spawning resources when a single damage source reaches the damage threshold.
/// </summary>
public sealed partial class CEOreVeinSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityTableSystem _table = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly Vector2[] Sides = [Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY];

    [SubscribeLocalEvent]
    private void OnDamageChanged(Entity<CEOreVeinComponent> ent, ref DamageChangedEvent args)
    {
        if (_net.IsClient || !args.DamageIncreased || args.DamageDelta is null)
            return;

        var gained = new DamageSpecifier(args.DamageDelta);
        gained.ClampMin(0);
        gained.TrimZeros();

        // Progress lives in the component, the wall itself never keeps damage
        _damageable.ClearAllDamage((ent.Owner, args.Damageable));

        foreach (var uid in ent.Comp.Progress.Keys)
        {
            if (!Exists(uid))
                ent.Comp.Progress.Remove(uid);
        }

        EntityUid? origin = args.Origin is { } o && Exists(o) ? o : null;

        var progress = gained;
        if (origin is null)
            progress += ent.Comp.UnknownProgress;
        else if (ent.Comp.Progress.TryGetValue(origin.Value, out var current))
            progress += current;

        var spawned = false;
        while (ThresholdMet(progress, ent.Comp.Damage))
        {
            var position = GetSpawnPosition(ent, origin);
            foreach (var loot in _table.GetSpawns(ent.Comp.Table))
            {
                SpawnAtPosition(loot, position);
            }

            progress -= ent.Comp.Damage;
            spawned = true;
        }

        progress.TrimZeros();
        if (origin is null)
            ent.Comp.UnknownProgress = progress;
        else
            ent.Comp.Progress[origin.Value] = progress;

        if (spawned)
            _audio.PlayPvs(ent.Comp.SpawnSound, ent);
    }

    private static bool ThresholdMet(DamageSpecifier progress, DamageSpecifier required)
    {
        foreach (var (type, amount) in required.DamageDict)
        {
            if (!progress.DamageDict.TryGetValue(type, out var actual) || actual < amount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Loot falls no further than one tile from the vein: towards the origin, or on a random side without one.
    /// </summary>
    private EntityCoordinates GetSpawnPosition(EntityUid vein, EntityUid? origin)
    {
        var veinXform = Transform(vein);

        if (origin is null)
            return veinXform.Coordinates.Offset(_random.Pick(Sides));

        var veinPos = _transform.GetMapCoordinates(veinXform);
        var originPos = _transform.GetMapCoordinates(origin.Value);
        if (veinPos.MapId != originPos.MapId)
            return veinXform.Coordinates.Offset(_random.Pick(Sides));

        var offset = originPos.Position - veinPos.Position;
        var length = offset.Length();
        if (length > 1f)
            offset /= length;

        return _transform.ToCoordinates(new MapCoordinates(veinPos.Position + offset, veinPos.MapId));
    }
}
