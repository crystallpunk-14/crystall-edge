using Content.Server._CE.PVS;
using Content.Shared._CE.ZLevels;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEZLevelsSystem
{
    /// <summary>
    /// creates a new entity zLevelNetwork
    /// </summary>
    [PublicAPI]
    public Entity<CEZLevelsNetworkComponent> CreateZNetwork()
    {
        var ent = Spawn();

        var zLevel = EnsureComp<CEZLevelsNetworkComponent>(ent);
        EnsureComp<CEPvsOverrideComponent>(ent);

        return (ent, zLevel);
    }

    /// <summary>
    /// attempts to add the specified map to the zNetwork network at the specified depth
    /// </summary>
    [PublicAPI]
    public bool TryAddMapIntoZNetwork(Entity<CEZLevelsNetworkComponent> network, EntityUid mapUid, int depth)
    {
        if (network.Comp.ZLevels.ContainsKey(depth))
        {
            Log.Error($"Failed to add map {mapUid} to ZLevelNetwork {network}: This depth is already occupied.");
            return false;
        }

        if (TryGetZNetwork(mapUid, out var otherNetwork))
        {
            Log.Error($"Failed attempt to add map {mapUid} to ZLevelNetwork {network}: This map is already in another network {otherNetwork}.");
            return false;
        }

        if (network.Comp.ZLevels.ContainsValue(mapUid))
        {
            Log.Error($"Failed attempt to add map {mapUid} to ZLevelNetwork {network} at depth {depth}: This map is already in this network.");
            return false;
        }

        network.Comp.ZLevels.Add(depth, mapUid);
        EnsureComp<CEZLevelMapComponent>(mapUid).Depth = depth;

        RaiseLocalEvent(mapUid, new CEMapAddedIntoZNetwork(mapUid, depth, network));

        return true;
    }
}

/// <summary>
/// Raised directly on map, when it is added into zLevel network
/// </summary>
public sealed class CEMapAddedIntoZNetwork(EntityUid mapUid, int depth, Entity<CEZLevelsNetworkComponent> network) : EntityEventArgs
{
    public EntityUid MapUid = mapUid;
    public int Depth = depth;
    public Entity<CEZLevelsNetworkComponent> Network = network;
}

/// <summary>
/// Raised directly on map, when it is removed from zLevel network
/// </summary>
public sealed class CEMapRemovedFromZNetwork(MapId mapId, int depth, Entity<CEZLevelsNetworkComponent> network) : EntityEventArgs
{
    public MapId MapId = mapId;
    public int Depth = depth;
    public Entity<CEZLevelsNetworkComponent> Network = network;
}
