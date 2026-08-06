using Content.Shared._CE.Hex;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.Science.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science;

public abstract partial class CESharedResearchTableSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private CEMagicEssenceSystem _essence = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEResearchTableComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<CEResearchTableComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
    }

    private void OnContainerChanged(Entity<CEResearchTableComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.PaperSlotId)
            return;

        _appearance.SetData(ent.Owner, CEResearchTableVisuals.HasPaper, true);
        OnPaperStateChanged(ent);
    }

    private void OnContainerChanged(Entity<CEResearchTableComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.PaperSlotId)
            return;

        _appearance.SetData(ent.Owner, CEResearchTableVisuals.HasPaper, false);
        OnPaperStateChanged(ent);
    }

    protected virtual void OnPaperStateChanged(Entity<CEResearchTableComponent> ent)
    {
    }

    // Flood-fills from one fixed target through only directly-related adjacent aspects, and checks
    // every other fixed target ended up reached - the puzzle's win condition. Shared so the client
    // can gate the "Finish Research" button and the server can validate the finish message against
    // the exact same rule.
    public bool IsProjectSolved(Dictionary<Vector2i, CEResearchMapTile> tiles)
    {
        var targets = new List<Vector2i>();
        foreach (var (hex, tile) in tiles)
        {
            if (tile.Fixed)
                targets.Add(hex);
        }

        if (targets.Count == 0)
            return false;

        var start = targets[0];
        var visited = new HashSet<Vector2i> { start };
        var queue = new Queue<Vector2i>();
        queue.Enqueue(start);

        while (queue.TryDequeue(out var hex))
        {
            var hexAspect = tiles[hex].Aspect!.Value;
            foreach (var neighbor in CEHexMath.Neighbors(hex))
            {
                if (visited.Contains(neighbor) ||
                    !tiles.TryGetValue(neighbor, out var tile) ||
                    tile.Aspect is not { } neighborAspect ||
                    !_essence.AreDirectlyRelated(hexAspect, neighborAspect))
                    continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        foreach (var target in targets)
        {
            if (!visited.Contains(target))
                return false;
        }

        return true;
    }

    // Legal if the tile is empty, non-dead, in-radius, and adjacent to at least one already-placed
    // aspect it's a direct recipe component of (or vice versa). Shared so the knowledge panel can
    // preview which held aspects are placeable at the selected tile using the exact same rule the
    // server validates the actual placement against.
    public bool CanPlaceAspect(
        Dictionary<Vector2i, CEResearchMapTile> tiles,
        int radius,
        Vector2i hex,
        ProtoId<CEMagicEssenceTypePrototype> essence)
    {
        if (CEHexMath.CubeDistance(hex, Vector2i.Zero) > radius)
            return false;

        if (tiles.TryGetValue(hex, out var existing) && (existing.DeadZone || existing.Aspect is not null))
            return false;

        foreach (var neighbor in CEHexMath.Neighbors(hex))
        {
            if (tiles.TryGetValue(neighbor, out var tile) &&
                tile.Aspect is { } neighborAspect &&
                _essence.AreDirectlyRelated(neighborAspect, essence))
                return true;
        }

        return false;
    }
}
