using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;

    private EntityQuery<MapComponent> _mapQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();

        _mapQuery = GetEntityQuery<MapComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();

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
    public bool TryMapOffset(MapId inputMapId,
        int offset,
        [NotNullWhen(true)] out MapId? outputMapId,
        [NotNullWhen(true)] out Entity<MapComponent>? outputMapUid)
    {
        outputMapId = null;
        outputMapUid = null;
        var query = EntityQueryEnumerator<CEZLevelsComponent>();
        while (query.MoveNext(out var zLevel))
        {
            if (!zLevel.ZLevels.TryGetValue(inputMapId, out var currentLevel))
                continue;

            var targetLevel = currentLevel + offset;

            if (!zLevel.ZLevels.ContainsValue(targetLevel))
                continue;

            foreach (var (key, value) in zLevel.ZLevels)
            {
                if (value == targetLevel && _map.TryGetMap(key, out var mapEntity) && _mapQuery.TryComp(mapEntity, out var mapComp))
                {
                    outputMapId = key;
                    outputMapUid = (mapEntity.Value, mapComp);
                    return true;
                }
            }
        }
        return false;
    }

    [PublicAPI]
    public bool TryMapUp(MapId imputMapId, [NotNullWhen(true)] out MapId? mapId, [NotNullWhen(true)] out Entity<MapComponent>? abobeMapUid)
    {
        return TryMapOffset(imputMapId, 1, out mapId, out abobeMapUid);
    }

    [PublicAPI]
    public bool TryMapDown(MapId imputMapId, [NotNullWhen(true)] out MapId? mapId, [NotNullWhen(true)] out Entity<MapComponent>? belowMapUid)
    {
        return TryMapOffset(imputMapId, -1, out mapId, out belowMapUid);
    }
}
