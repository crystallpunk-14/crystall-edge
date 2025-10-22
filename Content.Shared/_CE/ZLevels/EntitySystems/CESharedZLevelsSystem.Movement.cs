using Content.Shared.Ghost;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;

    private readonly TimeSpan _physicsUpdateDelay = TimeSpan.FromSeconds(0.1f);
    private TimeSpan _nextPhysicsUpdate = TimeSpan.Zero;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<GhostComponent> _ghostQuery;

    private void InitMovement()
    {
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _ghostQuery = GetEntityQuery<GhostComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        if (_timing.CurTime < _nextPhysicsUpdate)
            return;

        _nextPhysicsUpdate = _timing.CurTime + _physicsUpdateDelay;

        var query = EntityQueryEnumerator<TransformComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var xform, out var physics))
        {
            if (physics.BodyStatus == BodyStatus.InAir)
                continue;
            var map = xform.MapUid;
            if (_ghostQuery.HasComp(uid))
                continue;
            if (!_gridQuery.TryComp(map, out var mapGrid))
                continue;
            if (xform.ParentUid != xform.MapUid)
                continue;
            if (_map.TryGetTileRef(map.Value, mapGrid, _transform.GetWorldPosition(uid), out var tileRef) && !tileRef.Tile.IsEmpty)
                continue;

            TryMoveDown(uid);
        }
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
