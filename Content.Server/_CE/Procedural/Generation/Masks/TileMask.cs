using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Generation.Masks;

/// <summary>
/// Matches tiles whose current type is one of <see cref="Tiles"/> — the tile-as-mask handed down
/// from an earlier layer (e.g. a footprint painted by <see cref="Layers.TileNoiseDistanceLayer"/>).
/// </summary>
public sealed partial class TileMask : ICETileMask
{
    [DataField(required: true)]
    public List<ProtoId<ContentTileDefinition>> Tiles = new();

    private HashSet<int>? _tileIds;

    public override bool Matches(CEProceduralGenerationContext context, EntityUid map, MapGridComponent grid, Vector2i tile, Tile currentTile)
    {
        if (_tileIds is null)
        {
            _tileIds = new HashSet<int>(Tiles.Count);
            foreach (var t in Tiles)
            {
                _tileIds.Add(context.TileDefManager[t.Id].TileId);
            }
        }

        return _tileIds.Contains(currentTile.TypeId);
    }
}
