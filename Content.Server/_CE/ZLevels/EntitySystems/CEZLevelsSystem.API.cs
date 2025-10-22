using Content.Server._CE.PVS;
using Content.Shared._CE.ZLevels;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEZLevelsSystem
{
    private void InitApi()
    {

    }

    /// <summary>
    /// creates a new entity zLevelNetwork
    /// </summary>
    public Entity<CEZLevelsComponent> CreateZNetwork()
    {
        var ent = Spawn();

        var zLevel = EnsureComp<CEZLevelsComponent>(ent);
        EnsureComp<CEPvsOverrideComponent>(ent);

        return (ent, zLevel);
    }

    /// <summary>
    /// attempts to add the specified map to the zNetwork network at the specified depth
    /// </summary>
    [PublicAPI]
    public bool TryAddMapIntoZNetwork(Entity<CEZLevelsComponent> network, MapId mapId, int depth)
    {
        if (network.Comp.ZLevels.ContainsKey(mapId))
        {
            Log.Error($"Failed to add map {mapId} to ZLevelNetwork {network}: This map is already in this network.");
            return false;
        }

        if (TryGetZNetwork(mapId, out _, out var otherNetwork))
        {
            Log.Error($"Failed attempt to add map {mapId} to ZLevelNetwork {network}: This map is already in another network {otherNetwork}.");
            return false;
        }

        if (network.Comp.ZLevels.ContainsValue(depth))
        {
            Log.Error($"Failed attempt to add map {mapId} to ZLevelNetwork {network} at depth {depth}: This depth is already occupied.");
            return false;
        }

        network.Comp.ZLevels.Add(mapId, depth);
        return true;
    }

    [PublicAPI]
    public bool TryMove(EntityUid ent, int offset)
    {
        var xform = Transform(ent);
        var map = xform.MapUid;

        if (map is null)
            return false;

        if (!TryMapOffset(map.Value, offset, out var targetMap, out _))
            return false;

        _transform.SetMapCoordinates(ent, new MapCoordinates(_transform.GetWorldPosition(ent), targetMap.Value));
        return true;
    }

    [PublicAPI]
    public bool TryMoveUp(EntityUid ent)
    {
        return TryMove(ent, 1);
    }

    [PublicAPI]
    public bool TryMoveDown(EntityUid ent)
    {
        return TryMove(ent, -1);
    }
}
