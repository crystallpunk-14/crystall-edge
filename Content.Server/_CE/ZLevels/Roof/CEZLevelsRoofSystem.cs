/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.ZLevels.Roof;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.ZLevels.Roof;

/// <summary>
/// Server-only roof calculation. Clients must not recalculate networked roof data on their own:
/// they receive grid chunks of different z-levels in arbitrary order and would overwrite
/// server state with results computed from partial data.
/// </summary>
public sealed partial class CEZLevelsRoofSystem : CESharedZLevelsRoofSystem
{
    private readonly HashSet<Vector2i> _roofMap = new();
    private readonly HashSet<Vector2i> _weatherMap = new();

    [SubscribeLocalEvent]
    private void OnTileChanged(Entity<CEZLevelRoofComponent> ent, ref TileChangedEvent args)
    {
        if (!GridQuery.TryComp(ent, out var currentMapGrid))
            return;
        if (!RoofQuery.TryComp(ent, out var currentRoof))
            return;

        if (args.Changes.Length == 0)
            return;

        if (ZMapQuery.TryComp(ent, out var zLevelMapComp))
            OnMapTileChanged(ent, currentMapGrid, currentRoof, zLevelMapComp, args);
        else
            OnGridTileChanged(ent, currentMapGrid, currentRoof, args);
    }
}
