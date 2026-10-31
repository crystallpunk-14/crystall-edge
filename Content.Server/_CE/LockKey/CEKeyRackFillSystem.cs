using Content.Server._CE.ZLevels.Core;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared._CE.LockKey;
using Content.Shared._CE.LockKey.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.LockKey;

public sealed partial class CEKeyRackFillSystem : EntitySystem
{
    [Dependency] private CELockKeySystem _lockKey = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private StorageSystem _storage = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    /// <summary>
    /// Runs after z-levels are loaded, so locks on every station level are found.
    /// </summary>
    [SubscribeLocalEvent(after: [typeof(CEZLevelsSystem)])]
    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        var station = ev.Station.Owner;

        var lockTypes = new HashSet<ProtoId<CELockTypePrototype>>();
        // The main map is still paused (not map-initialized) here, so paused entities must be included
        var lockQuery = AllEntityQuery<CELockComponent>();
        while (lockQuery.MoveNext(out var uid, out var lockComp))
        {
            if (lockComp.CanEmbedded || lockComp.AutoGenerateShape is not { } type)
                continue;

            if (_station.GetOwningStation(uid) != station)
                continue;

            lockTypes.Add(type);
        }

        var rackQuery = AllEntityQuery<CEKeyRackFillComponent>();
        var racks = new List<Entity<CEKeyRackFillComponent>>();
        while (rackQuery.MoveNext(out var uid, out var rack))
        {
            if (_station.GetOwningStation(uid) == station)
                racks.Add((uid, rack));
        }

        foreach (var rack in racks)
        {
            FillRack(rack, lockTypes);
            RemCompDeferred<CEKeyRackFillComponent>(rack);
        }
    }

    private void FillRack(Entity<CEKeyRackFillComponent> rack, HashSet<ProtoId<CELockTypePrototype>> lockTypes)
    {
        var coords = Transform(rack).Coordinates;
        var types = new List<CELockTypePrototype>();
        foreach (var typeId in lockTypes)
        {
            if (!_proto.Resolve(typeId, out var type))
                continue;

            if (type.Group is not { } group || !rack.Comp.Groups.Contains(group))
                continue;

            types.Add(type);
        }

        // Stable order, so the rack looks the same every round
        types.Sort((a, b) => string.CompareOrdinal(a.ID, b.ID));

        foreach (var type in types)
        {
            var key = Spawn(rack.Comp.KeyProto, coords);
            if (!TryComp<CEKeyComponent>(key, out var keyComp))
            {
                Log.Error($"Key rack {ToPrettyString(rack)} key prototype {rack.Comp.KeyProto} has no {nameof(CEKeyComponent)}");
                Del(key);
                return;
            }

            _lockKey.TrySetShapeFromProto((key, keyComp), type.ID);

            if (!_storage.Insert(rack, key, out _, playSound: false))
                Log.Error($"Key rack {ToPrettyString(rack)} has no room for the {type.ID} key, dropped it nearby");
        }
    }
}
