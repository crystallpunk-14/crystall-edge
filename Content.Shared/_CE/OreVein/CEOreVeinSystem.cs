using System.Linq;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityTable;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._CE.OreVein;

/// <summary>
/// System that manages ore veins, spawning resources when specific damage thresholds are met.
/// </summary>
public sealed class CEOreVeinSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityTableSystem _table = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEOreVeinComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<CEOreVeinComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        var requiredDamage = ent.Comp.Damage;

        var totalDamage = args.Damageable.Damage;

        var allDamageTypesMet = true;
        foreach (var required in requiredDamage.DamageDict)
        {
            if (!totalDamage.DamageDict.TryGetValue(required.Key, out var actualAmount) || actualAmount < required.Value)
            {
                allDamageTypesMet = false;
                break;
            }
        }

        var targetSpawnPosition = Transform(ent).Coordinates;

        if (args.Origin is not null)
            targetSpawnPosition = Transform(args.Origin.Value).Coordinates;

        if (!allDamageTypesMet)
            return;

        foreach (var loot in _table.GetSpawns(ent.Comp.Table))
        {
            PredictedSpawnAtPosition(loot, targetSpawnPosition);
        }
        _damageable.ChangeDamage(ent.Owner, -requiredDamage);
        _audio.PlayPredicted(ent.Comp.SpawnSound, targetSpawnPosition, args.Origin);
    }
}
