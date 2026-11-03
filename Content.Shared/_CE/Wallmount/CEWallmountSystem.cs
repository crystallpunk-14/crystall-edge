using Content.Shared.Tag;
using Content.Shared.Wall;
using Robust.Shared.Analyzers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Wallmount;

public sealed partial class CEWallmountSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] private EntityQuery<CEWallmountComponent> _wallmountQuery = default!;
    [Dependency] private EntityQuery<CEWallmountedComponent> _wallmountedQuery = default!;
    [Dependency] private EntityQuery<WallComponent> _wallQuery = default!;

    //Upstream removed the "Wall" tag in favor of WallComponent; windows still use a tag
    public static readonly ProtoId<TagPrototype> WindowTag = "Window";

    [SubscribeLocalEvent]
    private void OnWallmountAnchorChanged(Entity<CEWallmountedComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            ClearWallmounts(ent);
    }

    [SubscribeLocalEvent]
    private void OnWallmountShutdown(Entity<CEWallmountedComponent> ent, ref ComponentShutdown args)
    {
        ClearWallmounts(ent);
    }

    [SubscribeLocalEvent]
    private void OnAttachedShutdown(Entity<CEWallmountComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Wall is { } wall && _wallmountedQuery.TryComp(wall, out var wallmounted))
            wallmounted.Attached.Remove(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<CEWallmountComponent>();
        while (query.MoveNext(out var uid, out var wallmount))
        {
            if (!wallmount.Initialized || wallmount.Wall != null)
                continue;

            if (_timing.CurTime < wallmount.NextAttachTime)
                continue;

            if (wallmount.AttachAttempts <= 0)
            {
                QueueDel(uid);
                continue;
            }

            wallmount.NextAttachTime = _timing.CurTime + TimeSpan.FromSeconds(0.5f);
            wallmount.AttachAttempts--;

            TryAttachWallmount((uid, wallmount));
        }
    }

    public bool IsWallSurface(EntityUid uid)
    {
        return _wallQuery.HasComp(uid) || _tag.HasTag(uid, WindowTag);
    }

    public bool HasWallmountAt(Entity<MapGridComponent> grid, Vector2i tile)
    {
        var enumerator = _map.GetAnchoredEntitiesEnumerator(grid, grid.Comp, tile);
        while (enumerator.MoveNext(out var uid))
        {
            if (_wallmountQuery.HasComp(uid.Value))
                return true;
        }

        return false;
    }

    private void ClearWallmounts(Entity<CEWallmountedComponent> ent)
    {
        foreach (var attached in ent.Comp.Attached)
        {
            QueueDel(attached);
        }

        ent.Comp.Attached.Clear();
    }

    private bool TryAttachWallmount(Entity<CEWallmountComponent> wallmount)
    {
        var grid = Transform(wallmount).GridUid;
        if (grid == null || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        //Try found a wall in neighbour tile
        var offset = Transform(wallmount).LocalRotation.ToWorldVec().Normalized();
        var targetPos = new EntityCoordinates(grid.Value, Transform(wallmount).LocalPosition - offset);
        var anchored = _map.GetAnchoredEntities(grid.Value, gridComp, targetPos);

        var hasParent = false;
        foreach (var entityUid in anchored)
        {
            if (!IsWallSurface(entityUid))
                continue;

            EnsureComp<CEWallmountedComponent>(entityUid, out var wallmounted);

            wallmounted.Attached.Add(wallmount);
            wallmount.Comp.Wall = entityUid;

            hasParent = true;
            break;
        }

        return hasParent;
    }
}
