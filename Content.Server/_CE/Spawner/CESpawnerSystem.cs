using System.Diagnostics;
using System.Linq;
using Content.Server._CE.Spawner.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Spawner;

public partial class CESpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEStationSpawnerDistributionComponent, StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(Entity<CEStationSpawnerDistributionComponent> ent, ref StationPostInitEvent args)
    {
        Timer.Spawn(TimeSpan.FromSeconds(1f), () => DistributeLoot(ent));
    }

    private void DistributeLoot(Entity<CEStationSpawnerDistributionComponent> ent)
    {
        Dictionary<ProtoId<TagPrototype>, List<EntProtoId>> plannedEntities = new();

        //Put default entities
        foreach (var (tag, entityTable) in ent.Comp.Spawns)
        {
            if (!plannedEntities.ContainsKey(tag))
                plannedEntities.Add(tag, new List<EntProtoId>());
            plannedEntities[tag].AddRange(_entityTable.GetSpawns(entityTable));
        }

        //Get dynamic entities from events or other systems
        var ev = new CEBeforeStationSpawnLootEvent(ent);
        RaiseLocalEvent(ent, ev, true);

        foreach (var (tag, entityList) in ev.Get())
        {
            if (!plannedEntities.ContainsKey(tag))
                plannedEntities.Add(tag, new List<EntProtoId>());
            plannedEntities[tag].AddRange(entityList);
        }

        //Start spawning
        HashSet<Entity<CESpawnerMarkerComponent>> allSpawners = new();
        var query = EntityQueryEnumerator<CESpawnerMarkerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            if (_station.GetOwningStation(uid) != ent)
                continue;

            allSpawners.Add((uid, spawner));
        }

        foreach (var (tag, entityList) in plannedEntities)
        {
            foreach (var proto in entityList)
            {
                // Try to find available spawners
                var candidates = allSpawners
                    .Where(sp => sp.Comp.WeightedTypes.ContainsKey(tag))
                    .ToHashSet();

                if (candidates.Count == 0)
                {
#if DEBUG
                    Debug.Assert(candidates.Count > 0, $"For the category of spawners [{tag.Id}], there are zero spawners. Items cannot be distributed across the map.");
#endif
                    candidates = allSpawners;
                }

                if (candidates.Count == 0)
                {
#if DEBUG
                    Debug.Assert(candidates.Count > 0, $"No spawner markers exist at all! Cannot spawn entity {proto} (tag={tag.Id}).");
#endif
                    break;
                }

                // Find minimal SpawnerCounter around markers
                var minCounter = candidates.Min(sp => sp.Comp.SpawnerCounter);

                // Select only markers with minimal counter
                var filtered = candidates
                    .Where(sp => sp.Comp.SpawnerCounter == minCounter)
                    .ToHashSet();

                // Build weights
                var weights = new Dictionary<Entity<CESpawnerMarkerComponent>, float>(filtered.Count);
                foreach (var sp in filtered)
                {
                    var baseWeight = sp.Comp.WeightedTypes.GetValueOrDefault(tag, 1f);
                    weights[sp] = baseWeight;
                }

                //  Select single spawner
                var chosen = WeightedPick(weights, _random);

                // Spawn
                var coords = Transform(chosen.Owner).Coordinates;
                SpawnAtPosition(proto, coords);

                chosen.Comp.SpawnerCounter++;
            }
        }
    }

    private static T WeightedPick<T>(
        Dictionary<T, float> weights,
        IRobustRandom random) where T : struct
    {
        if (weights.Count == 0)
            throw new InvalidOperationException("No candidates provided to WeightedPick!");

        var total = 0f;
        foreach (var w in weights.Values)
        {
            total += w;
        }

        var roll = random.NextFloat() * total;
        var cumulative = 0f;

        foreach (var (item, weight) in weights)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return item;
        }

        return weights.Keys.Last(); // fallback
    }
}

public sealed class CEBeforeStationSpawnLootEvent(EntityUid station) : EntityEventArgs
{
    private Dictionary<ProtoId<TagPrototype>, List<EntProtoId>> _additionalEntities = new();

    public EntityUid Station { get; } = station;

    public void Add(ProtoId<TagPrototype> tag, List<EntProtoId> additionalEntities)
    {
        if (!_additionalEntities.ContainsKey(tag))
            _additionalEntities.Add(tag, new List<EntProtoId>());

        _additionalEntities[tag].AddRange(additionalEntities);
    }

    public Dictionary<ProtoId<TagPrototype>, List<EntProtoId>> Get()
    {
        return _additionalEntities;
    }
}
