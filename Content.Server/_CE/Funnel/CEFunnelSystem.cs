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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEFunnelComponent>();
        while (query.MoveNext(out var uid, out var funnel))
        {
            if (_timing.CurTime < funnel.NextExtractionTime)
                continue;

            funnel.NextExtractionTime = _timing.CurTime + funnel.ExtractionFrequency;

            var ent = (uid, funnel);
            switch (GetMode(ent, out var conveyor))
            {
                case CEFunnelMode.Load:
                    RetryBlocked(ent);
                    break;
                case CEFunnelMode.Unload:
                    if (conveyor is { } conv && conv.Comp.Powered && conv.Comp.State != ConveyorState.Off)
                        ExtractNext(ent);
                    break;
            }
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
        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = MapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);

        // Wait until the previous item leaves the funnel tile, so a jammed belt doesn't pile items up
        var lying = new HashSet<Entity<ItemComponent>>();
        _lookup.GetLocalEntitiesIntersecting(gridUid, tile, lying, -0.05f, gridComp: grid);
        if (lying.Count > 0)
            return;

        var targetTile = tile.Offset(xform.LocalRotation.RotateDir(Direction.North));
        var anchored = MapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, targetTile);
        while (anchored.MoveNext(out var anchoredEntity))
        {
            if (HasComp<CEFunnelComponent>(anchoredEntity.Value))
                continue;

            if (!Container.TryGetContainer(anchoredEntity.Value, ent.Comp.ContainerCheckId, out var container))
                continue;

            foreach (var item in container.ContainedEntities)
            {
                if (!PassesFilter(ent, item))
                    continue;

                var extracted = TryComp<StackComponent>(item, out var stack) && stack.Count > 1
                    ? _stack.Split((item, stack), 1, xform.Coordinates) is not null
                    : Container.RemoveEntity(anchoredEntity.Value, item, destination: xform.Coordinates);

                if (!extracted)
                    continue;

                _audio.PlayPvs(ent.Comp.EjectSound, xform.Coordinates);
                return;
            }
        }
    }
}
