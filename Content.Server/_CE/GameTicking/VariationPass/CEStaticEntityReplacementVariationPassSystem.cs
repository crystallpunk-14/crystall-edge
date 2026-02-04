using System.Diagnostics;
using System.Linq;
using Content.Server._CE.GameTicking.VariationPass.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.VariationPass;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GameTicking.VariationPass;

/// <summary>
/// Variation pass system that replaces a fixed number of entities with their replacement prototypes.
/// This system searches for all entities matching the configured prototypes and randomly selects
/// a specified number of them to replace.
/// </summary>
public sealed class CEStaticEntityReplacementVariationPassSystem : VariationPassSystem<CEStaticEntityReplacementVariationPassComponent>
{
    /// <summary>
    ///     Used so we don't modify while enumerating
    ///     if the replaced entity also has <see cref="TEntComp"/>.
    ///
    ///     Filled and cleared within the same tick so no persistence issues.
    /// </summary>
    private readonly Queue<(string, EntityCoordinates, Angle)> _queuedSpawns = new();

    protected override void ApplyVariation(Entity<CEStaticEntityReplacementVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var comp = ent.Comp;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Collect all entities that can be replaced
        var candidateEntities = new List<Entity<TransformComponent>>();
        var query = AllEntityQuery<MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var meta, out var xform))
        {
            // Skip if not part of the station
            if (!IsMemberOfStation((uid, xform), ref args))
                continue;

            // Skip if entity has no prototype
            if (meta.EntityPrototype is null)
                continue;

            // Check if this entity's prototype is in our replacement map
            if (!comp.ReplacementMap.ContainsKey(meta.EntityPrototype.ID))
                continue;

            candidateEntities.Add((uid, xform));
        }

        // Determine how many entities we can actually replace
        var actualReplacementCount = Math.Min(comp.ReplacementCount, candidateEntities.Count);

        if (actualReplacementCount == 0)
            return;

        // Shuffle and select entities to replace
        Random.Shuffle(candidateEntities);
        var entitiesToReplace = candidateEntities.Take(actualReplacementCount).ToList();

        // Perform replacements
        foreach (var targetUid in entitiesToReplace)
        {
            // Skip if entity was already deleted by a prior variation pass
            if (!Exists(targetUid))
                continue;

            var targetMeta = MetaData(targetUid);
            if (targetMeta.EntityPrototype is null)
                continue;

            // Get replacement prototype
            if (!comp.ReplacementMap.TryGetValue(targetMeta.EntityPrototype.ID, out var replacementProto))
                continue;

            // Delete the original entity
            Replace(targetUid, replacementProto);
        }

        while (_queuedSpawns.TryDequeue(out var tup))
        {
            var (spawn, coords, rot) = tup;
            var newEnt = Spawn(spawn, coords);
            Transform(newEnt).LocalRotation = rot;
        }

        Log.Debug($"Static entity replacement took {stopwatch.Elapsed} with {Stations.GetTileCount(args.Station.AsNullable())} tiles");
    }

    private void Replace(Entity<TransformComponent> ent, EntProtoId replacement)
    {
        var coords = ent.Comp.Coordinates;
        var rot = ent.Comp.LocalRotation;
        Del(ent);

        _queuedSpawns.Enqueue((replacement, coords, rot));
    }
}
