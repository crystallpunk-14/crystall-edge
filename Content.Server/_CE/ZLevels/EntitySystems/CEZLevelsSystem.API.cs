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
        EnsureComp<CEZLevelMapComponent>(_map.GetMap(mapId));

        RaiseLocalEvent(_map.GetMap(mapId), new CEMapAddedIntoZNetwork(mapId, depth, network));

        return true;
    }
}

/// <summary>
/// Raised directly on map, when it is added into zLevel network
/// </summary>
public sealed class CEMapAddedIntoZNetwork(MapId mapId, int depth, Entity<CEZLevelsComponent> network) : EntityEventArgs
{
    public MapId MapId = mapId;
    public int Depth = depth;
    public Entity<CEZLevelsComponent> Network = network;
}

/// <summary>
/// Raised directly on map, when it is removed from zLevel network
/// </summary>
public sealed class CEMapRemovedFromZNetwork(MapId mapId, int depth, Entity<CEZLevelsComponent> network) : EntityEventArgs
{
    public MapId MapId = mapId;
    public int Depth = depth;
    public Entity<CEZLevelsComponent> Network = network;
}
