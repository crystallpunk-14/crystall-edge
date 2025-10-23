using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitMovement();
        InitRoof();
    }

    /// <summary>
    /// Checks whether the map is in the zLevels network. If so, returns true and the current depth + Entity of the current zLevels network.
    /// </summary>
    [PublicAPI]
    public bool TryGetZNetwork(MapId mapId,[NotNullWhen(true)] out int? depth, [NotNullWhen(true)] out Entity<CEZLevelsComponent>? zLevel)
    {
        depth = null;
        zLevel = null;
        var query = EntityQueryEnumerator<CEZLevelsComponent>();
        while (query.MoveNext(out var uid, out var zLevelComp))
        {
            if (zLevelComp.ZLevels.TryGetValue(mapId, out var foundedDepth))
            {
                depth = foundedDepth;
                zLevel = (uid, zLevelComp);
                return true;
            }
        }

        return false;
    }

    [PublicAPI]
    public bool TryMapOffset(EntityUid mapUid, int offset, [NotNullWhen(true)] out MapId? mapId,  [NotNullWhen(true)] out EntityUid? outputMapUid)
    {
        mapId = null;
        outputMapUid = null;
        var query = EntityQueryEnumerator<CEZLevelsComponent>();
        while (query.MoveNext(out var zLevel))
        {
            if (!zLevel.ZLevels.TryGetValue(Transform(mapUid).MapID, out var currentLevel))
                continue;

            var targetLevel = currentLevel + offset;

            if (!zLevel.ZLevels.ContainsValue(targetLevel))
                continue;

            foreach (var (key, value) in zLevel.ZLevels)
            {
                if (value == targetLevel && _map.MapExists(key))
                {
                    mapId = key;
                    outputMapUid = _map.GetMap(key);
                    return true;
                }
            }
        }
        return false;
    }

    [PublicAPI]
    public bool TryMapUp(EntityUid mapUid, [NotNullWhen(true)] out MapId? mapId, [NotNullWhen(true)] out EntityUid? abobeMapUid)
    {
        return TryMapOffset(mapUid, 1, out mapId, out abobeMapUid);
    }

    [PublicAPI]
    public bool TryMapDown(EntityUid mapUid, [NotNullWhen(true)] out MapId? mapId, [NotNullWhen(true)] out EntityUid? belowMapUid)
    {
        return TryMapOffset(mapUid, -1, out mapId, out belowMapUid);
    }

    [PublicAPI]
    public List<EntityUid> GetAllMapsBelow(EntityUid mapUid)
    {
        List<EntityUid> mapIds = new();
        var query = EntityQueryEnumerator<CEZLevelsComponent>();
        while (query.MoveNext(out var zLevel))
        {
            if (!zLevel.ZLevels.TryGetValue(Transform(mapUid).MapID, out var currentDepth))
                continue;

            foreach (var (map, depth) in zLevel.ZLevels)
            {
                if (depth >= currentDepth)
                    continue;

                mapIds.Add(_map.GetMap(map));
            }
            break;
        }

        return mapIds;
    }
}
