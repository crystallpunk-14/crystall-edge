using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.Procedural.Generation.Masks;

/// <summary>
/// One condition tested against a tile by a procedural layer — selected in YAML via <c>!type:</c>.
/// A layer combines several masks with AND, each contributing <see cref="Matches"/> XOR
/// <see cref="Inverted"/>, so composing several conditions never needs a switch or bespoke boolean
/// plumbing in the layer itself. <see cref="Inverted"/> lives here so every mask inherits it instead
/// of redeclaring the same field.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class ICETileMask
{
    /// <summary>
    /// Flips this mask's result — "matches everywhere except" instead of "matches only where".
    /// </summary>
    [DataField]
    public bool Inverted { get; set; }

    /// <summary>
    /// <paramref name="currentTile"/> is the caller's already-fetched tile at <paramref name="tile"/> —
    /// implementations that only care about the tile's type should read it from here instead of doing
    /// their own grid lookup, since the caller already paid for one while enumerating the grid.
    /// </summary>
    public abstract bool Matches(CEProceduralGenerationContext context, EntityUid map, MapGridComponent grid, Vector2i tile, Tile currentTile);

    /// <summary>
    /// Snapshots every existing tile of <paramref name="map"/> that lies inside the context's region
    /// and passes all <paramref name="masks"/>.
    /// </summary>
    public static List<Vector2i> CollectTiles(
        CEProceduralGenerationContext context,
        EntityUid map,
        MapGridComponent grid,
        List<ICETileMask> masks)
    {
        var targets = new List<Vector2i>();
        var enumerator = context.Map.GetAllTilesEnumerator(map, grid);
        while (enumerator.MoveNext(out var tileRef))
        {
            var indices = tileRef.Value.GridIndices;
            if (!context.Region.Contains(indices))
                continue;

            var passes = true;
            foreach (var mask in masks)
            {
                if (mask.Matches(context, map, grid, indices, tileRef.Value.Tile) == mask.Inverted)
                {
                    passes = false;
                    break;
                }
            }

            if (passes)
                targets.Add(indices);
        }

        return targets;
    }
}
