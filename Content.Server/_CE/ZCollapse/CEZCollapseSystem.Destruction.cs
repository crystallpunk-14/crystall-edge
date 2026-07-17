using Content.Shared.Destructible;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.ZCollapse;

// Entities anchored to a tile that collapses get force-destroyed through the normal Destructible
// pipeline (break sound, rubble, etc. — whatever each entity is configured to do) instead of just
// silently vanishing along with the tile.
public sealed partial class CEZCollapseSystem
{
    [Dependency] private SharedDestructibleSystem _destructible = default!;

    private void DestroyAnchoredEntities(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        List<EntityUid>? anchored = null;
        while (enumerator.MoveNext(out var ent))
        {
            anchored ??= new List<EntityUid>();
            anchored.Add(ent.Value);
        }

        if (anchored == null)
            return;

        foreach (var uid in anchored)
        {
            _destructible.DestroyEntity(uid);
        }
    }
}