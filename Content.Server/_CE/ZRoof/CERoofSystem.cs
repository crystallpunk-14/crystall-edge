using System.Linq;
using Content.Server._CE.ZLevels.EntitySystems;
using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZRoof;
using Content.Shared.Light.Components;

namespace Content.Server._CE.ZRoof;

/// <inheritdoc/>
public sealed class CERoofSystem : CESharedRoofSystem
{
    private readonly HashSet<Vector2i> _roofMap = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZLevelsNetworkComponent, CEZLevelNetworkUpdatedEvent>(OnNetworkUpdated);
    }

    private void OnNetworkUpdated(Entity<CEZLevelsNetworkComponent> ent, ref CEZLevelNetworkUpdatedEvent args)
    {
        RecalculateNetworkRoofs(ent);
    }

    public void RecalculateNetworkRoofs(Entity<CEZLevelsNetworkComponent> network)
    {
        _roofMap.Clear();

        List<EntityUid> sortedMaps = new();
        foreach (var mapUid in network.Comp.ZLevels
                     .OrderByDescending(kv => kv.Key) // depth sorting
                     .Select(kv => kv.Value)
                     .Where(uid => uid.HasValue)
                     .Select(uid => uid!.Value))
        {
            sortedMaps.Add(mapUid);
        }

        foreach (var map in sortedMaps)
        {
            if (!GridQuery.TryComp(map, out var mapGrid))
                continue;

            var enumerator = Map.GetAllTilesEnumerator(map, mapGrid);
            var roofComp = EnsureComp<RoofComponent>(map);

            while (enumerator.MoveNext(out var tileRef))
            {
                Roof.SetRoof((map, mapGrid, roofComp), tileRef.Value.GridIndices, _roofMap.Contains(tileRef.Value.GridIndices));

                if (!tileRef.Value.Tile.IsEmpty)
                    _roofMap.Add(tileRef.Value.GridIndices);
            }
        }
    }
}
