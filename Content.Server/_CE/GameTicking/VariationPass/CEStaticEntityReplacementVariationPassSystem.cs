using System.Linq;
using Content.Server._CE.GameTicking.VariationPass.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.VariationPass;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Server._CE.GameTicking.VariationPass;

/// <summary>
/// Variation pass system that replaces a fixed number of entities with their replacement prototypes.
/// This system searches for all entities matching the configured prototypes and randomly selects
/// a specified number of them to replace.
/// </summary>
public sealed class CEStaticEntityReplacementVariationPassSystem : VariationPassSystem<CEStaticEntityReplacementVariationPassComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    protected override void ApplyVariation(Entity<CEStaticEntityReplacementVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var comp = ent.Comp;

        // Collect all entities that can be replaced
        var candidateEntities = new List<EntityUid>();
        var query = EntityQueryEnumerator<MetaDataComponent, TransformComponent>();

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

            // Apply whitelist filtering if configured
            if (comp.Whitelist is not null && !_whitelistSystem.IsWhitelistPass(comp.Whitelist, uid))
                continue;

            // Apply blacklist filtering if configured
            if (comp.Blacklist is not null && _whitelistSystem.IsWhitelistPass(comp.Blacklist, uid))
                continue;

            candidateEntities.Add(uid);
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
            var targetMeta = MetaData(targetUid);
            if (targetMeta.EntityPrototype is null)
                continue;

            // Get replacement prototype
            if (!comp.ReplacementMap.TryGetValue(targetMeta.EntityPrototype.ID, out var replacementProto))
                continue;

            // Get the coordinates before deleting
            var coordinates = Transform(targetUid).Coordinates;

            // Spawn replacement entity at the same location
            SpawnAtPosition(replacementProto, coordinates);

            // Delete the original entity
            QueueDel(targetUid);
        }
    }
}
