using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Funnel;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server._CE.Funnel;

public sealed partial class CEFunnelSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEFunnelComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<CEFunnelActivatorComponent, PowerChangedEvent>(OnPowerChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEFunnelComponent>();
        while (query.MoveNext(out var uid, out var funnel))
        {
            if (!funnel.ActiveExtraction)
                continue;

            if (!HasActiveActivator((uid, funnel)))
            {
                funnel.ActiveExtraction = false;
                continue;
            }

            if (_timing.CurTime < funnel.NextExtractionTime)
                continue;

            funnel.NextExtractionTime = _timing.CurTime + funnel.ExtractionFrequency;

            ExtractNext((uid, funnel));
        }
    }

    private void OnPowerChanged(Entity<CEFunnelActivatorComponent> ent, ref PowerChangedEvent args)
    {
        var xform = Transform(ent);

        if (xform.GridUid == null)
            return;

        var gridUid = xform.GridUid.Value;
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var currentTile = _mapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);

        // Get the direction the activator is facing
        var activatorDirection = xform.LocalRotation.GetCardinalDir();

        // Iterate through all anchored entities on the same tile
        var anchoredEnumerator = _mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, currentTile);
        while (anchoredEnumerator.MoveNext(out var anchoredEntity))
        {
            if (!TryComp<CEFunnelComponent>(anchoredEntity.Value, out var funnelComp))
                continue;

            // Get the direction the funnel is facing
            var funnelXform = Transform(anchoredEntity.Value);
            var funnelDirection = funnelXform.LocalRotation.GetCardinalDir();

            // Only activate extraction if directions match
            if (funnelDirection != activatorDirection.GetOpposite())
                funnelComp.ActiveExtraction = args.Powered;
        }
    }

    private void OnStartCollide(Entity<CEFunnelComponent> ent, ref StartCollideEvent args)
    {
        if (!_whitelist.CheckBoth(args.OtherEntity, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        TryContain(ent, args.OtherEntity);
    }

    private bool TryContain(Entity<CEFunnelComponent> ent, EntityUid target)
    {
        var xform = Transform(ent);

        if (xform.GridUid == null)
            return false;

        var gridUid = xform.GridUid.Value;
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var currentTile = _mapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);

        var direction = xform.LocalRotation.RotateDir(Direction.North);
        var targetTile = currentTile.Offset(direction);

        var anchoredEnumerator = _mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, targetTile);
        while (anchoredEnumerator.MoveNext(out var anchoredEntity))
        {
            if (!TryComp<StorageComponent>(anchoredEntity.Value, out var storage))
                continue;

            if (_storage.Insert(anchoredEntity.Value, target, out _, user: null, storage))
            {
                _audio.PlayPredicted(ent.Comp.InsertSound, xform.Coordinates, null);
                return true;
            }
        }

        return false;
    }

    private void ExtractNext(Entity<CEFunnelComponent> ent)
    {
        if (_net.IsClient)
            return;

        var xform = Transform(ent);

        if (xform.GridUid == null)
            return;

        var gridUid = xform.GridUid.Value;
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var currentTile = _mapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);

        var direction = xform.LocalRotation.RotateDir(Direction.North);
        var targetTile = currentTile.Offset(direction);

        var anchoredEnumerator = _mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, targetTile);
        while (anchoredEnumerator.MoveNext(out var anchoredEntity))
        {
            if (!TryComp<StorageComponent>(anchoredEntity.Value, out var storage))
                continue;

            if (storage.Container.ContainedEntities.Count == 0)
                continue;

            var itemToExtract = storage.Container.ContainedEntities[0];

            if (!_whitelist.CheckBoth(itemToExtract, ent.Comp.Blacklist, ent.Comp.Whitelist))
                continue;

            if (_container.RemoveEntity(anchoredEntity.Value, itemToExtract, destination: xform.Coordinates))
            {
                _audio.PlayPredicted(ent.Comp.EjectSound, xform.Coordinates, null);
                return;
            }
        }
    }

    private bool HasActiveActivator(Entity<CEFunnelComponent> ent)
    {
        var xform = Transform(ent);

        if (xform.GridUid == null)
            return false;

        var gridUid = xform.GridUid.Value;
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var currentTile = _mapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var funnelDirection = xform.LocalRotation.GetCardinalDir();

        var anchoredEnumerator = _mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, currentTile);
        while (anchoredEnumerator.MoveNext(out var anchoredEntity))
        {
            if (!TryComp<CEFunnelActivatorComponent>(anchoredEntity.Value, out _))
                continue;

            if (!this.IsPowered(anchoredEntity.Value, EntityManager))
                continue;

            var activatorXform = Transform(anchoredEntity.Value);
            var activatorDirection = activatorXform.LocalRotation.GetCardinalDir();

            if (funnelDirection != activatorDirection.GetOpposite())
                return true;
        }

        return false;
    }
}
