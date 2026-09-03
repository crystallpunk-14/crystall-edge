using System.Threading.Tasks;
using Content.Server._CE.Procedural.Generation.Masks;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Generation.Layers;

/// <summary>
/// Anchors one <see cref="Entity"/> onto every tile of the map that passes <see cref="Mask"/> —
/// e.g. an ore vein: a <see cref="CENoiseMask"/> for where the vein runs, an <see cref="CEEntityMask"/>
/// to only touch tiles that already have a stone wall. Exactly one anchored entity is meant to survive
/// on a tile by the end of generation, so whatever's already anchored there is deleted first.
/// </summary>
public sealed partial class CEFillEntityLayer : ICEProceduralLayer
{
    [DataField(required: true)]
    public EntProtoId Entity;

    [DataField(required: true)]
    public List<ICETileMask> Mask = new();

    public async Task Apply(CEProceduralGenerationContext context, EntityUid map)
    {
        var grid = context.EntityManager.GetComponent<MapGridComponent>(map);
        var transform = context.EntityManager.System<SharedTransformSystem>();

        var targets = new List<Vector2i>();
        var enumerator = context.Map.GetAllTilesEnumerator(map, grid);
        while (enumerator.MoveNext(out var tileRef))
        {
            var indices = tileRef.Value.GridIndices;

            var passes = true;
            foreach (var tileMask in Mask)
            {
                if (tileMask.Matches(context, map, grid, indices, tileRef.Value.Tile) == tileMask.Inverted)
                {
                    passes = false;
                    break;
                }
            }

            if (passes)
                targets.Add(indices);
        }

        var processed = 0;
        foreach (var indices in targets)
        {
            var anchored = context.Map.GetAnchoredEntitiesEnumerator(map, grid, indices);
            while (anchored.MoveNext(out var ent))
            {
                context.EntityManager.QueueDeleteEntity(ent.Value);
            }

            var coords = context.Map.GridTileToLocal(map, grid, indices);
            var spawned = context.EntityManager.SpawnEntity(Entity, coords);
            var spawnedXform = context.EntityManager.GetComponent<TransformComponent>(spawned);
            transform.AnchorEntity((spawned, spawnedXform), (map, grid));

            if (++processed % 64 == 0)
            {
                await context.Suspend();
                context.Cancellation.ThrowIfCancellationRequested();
            }
        }
    }
}
