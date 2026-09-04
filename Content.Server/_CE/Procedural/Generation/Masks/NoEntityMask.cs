using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.Procedural.Generation.Masks;

/// <summary>
/// Matches tiles with no anchored entity at all.
/// </summary>
public sealed partial class NoEntityMask : ICETileMask
{
    public override bool Matches(CEProceduralGenerationContext context, EntityUid map, MapGridComponent grid, Vector2i tile, Tile currentTile)
    {
        var anchored = context.Map.GetAnchoredEntitiesEnumerator(map, grid, tile);
        return !anchored.MoveNext(out _);
    }
}
