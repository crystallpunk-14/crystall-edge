using Content.Server.Storage.EntitySystems;
using Content.Shared._CE.Funnel;
using Content.Shared.Conveyor;
using Content.Shared.Item;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server._CE.Funnel;

public sealed partial class CEFunnelSystem : CESharedFunnelSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StorageSystem _storage = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    private static readonly Direction[] Cardinals = [Direction.North, Direction.South, Direction.East, Direction.West];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEFunnelComponent>();
        while (query.MoveNext(out var uid, out var funnel))
        {
            if (_timing.CurTime < funnel.NextExtractionTime)
                continue;

            funnel.NextExtractionTime = _timing.CurTime + funnel.ExtractionFrequency;

            Entity<CEFunnelComponent> ent = (uid, funnel);
            if (GetMode(ent, out _) == CEFunnelMode.Load)
                RetryBlocked(ent);
            else
                ExtractNext(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnStartCollide(Entity<CEFunnelComponent> ent, ref StartCollideEvent args)
    {
        var mode = GetMode(ent, out _);

        if (args.OurFixtureId == ent.Comp.FixtureId && mode == CEFunnelMode.Load ||
            args.OurFixtureId == ent.Comp.SideFixtureId && mode == CEFunnelMode.Side)
            TryContain(ent, args.OtherEntity);
    }

    /// <summary>
    /// Items stopped by the front edge get another try, e.g. once the container has free space.
    /// </summary>
    private void RetryBlocked(Entity<CEFunnelComponent> ent)
    {
        var front = Fixtures.GetFixtureOrNull(ent, ent.Comp.FixtureId);
        if (front is null)
            return;

        var blocked = new List<EntityUid>();
        var contacts = _physics.GetContacts(ent.Owner);
        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            var ours = contact.EntityA == ent.Owner ? contact.FixtureA : contact.FixtureB;
            if (ours != front)
                continue;

            blocked.Add(contact.EntityA == ent.Owner ? contact.EntityB : contact.EntityA);
        }

        foreach (var item in blocked)
        {
            TryContain(ent, item);
        }
    }

    private bool TryContain(Entity<CEFunnelComponent> ent, EntityUid target)
    {
        if (!HasComp<ItemComponent>(target) || !PassesFilter(ent, target))
            return false;

        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var targetTile = MapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates)
            .Offset(xform.LocalRotation.RotateDir(Direction.North));

        var anchored = MapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, targetTile);
        while (anchored.MoveNext(out var anchoredEntity))
        {
            // Don't stuff items into another funnel's filter
            if (HasComp<CEFunnelComponent>(anchoredEntity.Value))
                continue;

            if (!Container.TryGetContainer(anchoredEntity.Value, ent.Comp.ContainerCheckId, out var container))
                continue;

            var success = ent.Comp.ContainerCheckId == StorageComponent.ContainerId
                ? _storage.Insert(anchoredEntity.Value, target, out _, user: null)
                : Container.Insert(target, container);

            if (!success)
                continue;

            _audio.PlayPvs(ent.Comp.InsertSound, xform.Coordinates);
            return true;
        }

        return false;
    }

    private void ExtractNext(Entity<CEFunnelComponent> ent)
    {
        if (!TryGetExtractable(ent, out var source, out var item))
            return;

        // Funnels on the same container take turns: the one idle for the longest goes first
        if (HasOlderReadyPeer(ent, source))
            return;

        var coords = Transform(ent).Coordinates;
        var extracted = TryComp<StackComponent>(item, out var stack) && stack.Count > 1
            ? _stack.Split((item, stack), 1, coords) is not null
            : Container.RemoveEntity(source, item, destination: coords);

        if (!extracted)
            return;

        ent.Comp.LastExtractionTime = _timing.CurTime;
        _audio.PlayPvs(ent.Comp.EjectSound, coords);
    }

    /// <summary>
    /// Checks that the funnel unloads through a powered conveyor onto a free tile and finds the first item matching its filter.
    /// </summary>
    private bool TryGetExtractable(Entity<CEFunnelComponent> ent, out EntityUid source, out EntityUid item)
    {
        source = default;
        item = default;

        if (GetMode(ent, out var conveyor) != CEFunnelMode.Unload ||
            conveyor is not { } conv ||
            !conv.Comp.Powered ||
            conv.Comp.State == ConveyorState.Off)
            return false;

        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tile = MapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);

        // Wait until the previous item leaves the funnel tile, so a jammed belt doesn't pile items up
        var lying = new HashSet<Entity<ItemComponent>>();
        _lookup.GetLocalEntitiesIntersecting(gridUid, tile, lying, -0.05f, gridComp: grid);
        if (lying.Count > 0)
            return false;

        var targetTile = tile.Offset(xform.LocalRotation.RotateDir(Direction.North));
        var anchored = MapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, targetTile);
        while (anchored.MoveNext(out var anchoredEntity))
        {
            if (HasComp<CEFunnelComponent>(anchoredEntity.Value))
                continue;

            if (!Container.TryGetContainer(anchoredEntity.Value, ent.Comp.ContainerCheckId, out var container))
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                if (!PassesFilter(ent, contained))
                    continue;

                source = anchoredEntity.Value;
                item = contained;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True if another funnel unloading the same container is ready and has been idle for longer.
    /// </summary>
    private bool HasOlderReadyPeer(Entity<CEFunnelComponent> ent, EntityUid source)
    {
        var sourceXform = Transform(source);
        if (sourceXform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var sourceTile = MapSystem.TileIndicesFor(gridUid, grid, sourceXform.Coordinates);
        foreach (var dir in Cardinals)
        {
            var anchored = MapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, sourceTile.Offset(dir));
            while (anchored.MoveNext(out var peerUid))
            {
                if (peerUid == ent.Owner || !TryComp<CEFunnelComponent>(peerUid, out var peer))
                    continue;

                if (peer.LastExtractionTime >= ent.Comp.LastExtractionTime)
                    continue;

                if (TryGetExtractable((peerUid.Value, peer), out var peerSource, out _) && peerSource == source)
                    return true;
            }
        }

        return false;
    }
}
