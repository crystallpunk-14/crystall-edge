using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared._CE.ItemReceiver;

public sealed partial class CEItemReceiverSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEItemReceiverComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<CEItemReceiverComponent> ent, ref StartCollideEvent args)
    {
        if (!_whitelist.CheckBoth(args.OtherEntity, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        TryContain(ent, args.OtherEntity);
    }

    private bool TryContain(Entity<CEItemReceiverComponent> ent, EntityUid target)
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
}
