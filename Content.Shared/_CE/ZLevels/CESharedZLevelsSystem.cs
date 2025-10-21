using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared._CE.ZLevels;

public abstract partial class CESharedZLevelsSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;

    [PublicAPI]
    public bool TryMapOffset(EntityUid mapUid, int offset, [NotNullWhen(true)] out MapId? mapId)
    {
        mapId = null;
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
                    return true;
                }
            }
        }
        return false;
    }

    [PublicAPI]
    public bool TryMapUp(EntityUid mapUid, [NotNullWhen(true)] out MapId? mapId)
    {
        return TryMapOffset(mapUid, 1, out mapId);
    }

    [PublicAPI]
    public bool TryMapDown(EntityUid mapUid, [NotNullWhen(true)] out MapId? mapId)
    {
        return TryMapOffset(mapUid, -1, out mapId);
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
