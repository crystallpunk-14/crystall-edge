using Content.Server._CE.Procedural.Generation.Masks;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Generation.Layers;

/// <summary>
/// Anchors one <see cref="Entity"/> onto every tile of the map that passes <see cref="Mask"/> —
/// e.g. an ore vein: a <see cref="NoiseMask"/> for where the vein runs, an <see cref="EntityMask"/>
/// to only touch tiles that already have a stone wall. Exactly one anchored entity is meant to survive
/// on a tile by the end of generation, so whatever's already anchored there is deleted first.
/// </summary>
public sealed partial class FillEntityLayer : ICEProceduralLayer
{
    [DataField(required: true)]
    public EntProtoId Entity;

    [DataField(required: true)]
    public List<ICETileMask> Mask = new();

    public void Apply(CEProceduralGenerationContext context, EntityUid map)
    {
        var grid = context.EntityManager.GetComponent<MapGridComponent>(map);
        var transform = context.EntityManager.System<SharedTransformSystem>();

        var targets = ICETileMask.CollectTiles(context, map, grid, Mask);

        var toDelete = new List<EntityUid>();
        foreach (var indices in targets)
        {
            // Deleted immediately rather than queued: later layers in the same pass must not see them.
            toDelete.Clear();
            var anchored = context.Map.GetAnchoredEntitiesEnumerator(map, grid, indices);
            while (anchored.MoveNext(out var ent))
            {
                toDelete.Add(ent.Value);
            }

            foreach (var ent in toDelete)
            {
                context.EntityManager.DeleteEntity(ent);
            }

            var coords = context.Map.GridTileToLocal(map, grid, indices);
            var spawned = context.EntityManager.SpawnEntity(Entity, coords);
            var spawnedXform = context.EntityManager.GetComponent<TransformComponent>(spawned);
        }
    }
}
