using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Generation.Masks;

/// <summary>
/// Matches tiles with an anchored entity whose prototype is one of <see cref="Entities"/> — e.g.
/// "only tiles that already have a stone wall", to turn generic biome walls into ore.
/// </summary>
public sealed partial class EntityMask : ICETileMask
{
    [DataField(required: true)]
    public List<EntProtoId> Entities = new();

    public override bool Matches(CEProceduralGenerationContext context, EntityUid map, MapGridComponent grid, Vector2i tile, Tile currentTile)
    {
        var anchored = context.Map.GetAnchoredEntitiesEnumerator(map, grid, tile);
        while (anchored.MoveNext(out var ent))
        {
            if (context.EntityManager.TryGetComponent<MetaDataComponent>(ent.Value, out var meta) &&
                meta.EntityPrototype is { } proto &&
                Entities.Contains(proto.ID))
                return true;
        }

        return false;
    }
}
