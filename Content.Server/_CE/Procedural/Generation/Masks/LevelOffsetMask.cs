using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.Procedural.Generation.Masks;

/// <summary>
/// Runs <see cref="Masks"/> against the same tile on the z-level <see cref="Offset"/> levels away — the way to say
/// "only where the cave below is open". Never matches when that level doesn't exist.
/// </summary>
public abstract partial class LevelOffsetMask : ICETileMask
{
    [DataField(required: true)]
    public List<ICETileMask> Masks = new();

    protected abstract int Offset { get; }

    public override bool Matches(CEProceduralGenerationContext context, EntityUid map, MapGridComponent grid, Vector2i tile, Tile currentTile)
    {
        var entMan = context.EntityManager;
        var zLevels = entMan.System<CESharedZLevelsSystem>();

        if (!zLevels.TryMapOffset(new Entity<CEZMapComponent?>(map, null), Offset, out var other) ||
            !entMan.TryGetComponent<MapGridComponent>(other, out var otherGrid))
            return false;

        var otherTile = context.Map.GetTileRef(other.Owner, otherGrid, tile).Tile;
        return PassesAll(context, other.Owner, otherGrid, tile, otherTile, Masks);
    }
}
