using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.TileEditTool;

public sealed class CEEditTileToolSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEEditTileToolComponent, AfterInteractEvent>(OnTileClick);
        SubscribeLocalEvent<CEEditTileToolComponent, CETileToolReplaceDoAfter>(OnDoAfterEnd);
    }

    private void OnTileClick(Entity<CEEditTileToolComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (TryGetReplaceTile(ent, args.ClickLocation) == null)
            return;

        var doAfterArgs =
            new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.Delay,
                new CETileToolReplaceDoAfter(GetNetCoordinates(args.ClickLocation)),
                ent)
            {
                BreakOnDamage = true,
                BlockDuplicate = false,
                CancelDuplicate = false,
                BreakOnMove = true,
                BreakOnHandChange = true,
            };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private ProtoId<ContentTileDefinition>? TryGetReplaceTile(Entity<CEEditTileToolComponent> ent, EntityCoordinates location)
    {
        var map = _transform.GetMap(location);
        if (!TryComp<MapGridComponent>(map, out var gridComp))
            return null;

        var tileRef = _map.GetTileRef(map.Value, gridComp, location);
        var tile = _turf.GetContentTileDefinition(tileRef);

        if (!ent.Comp.TileReplace.TryGetValue(tile, out var replaceTile))
            return null;

        if (_map.GetAnchoredEntities((map.Value, gridComp), location).Any())
            return null;

        return replaceTile;
    }

    private void OnDoAfterEnd(Entity<CEEditTileToolComponent> ent, ref CETileToolReplaceDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var location = GetCoordinates(args.Coordinates);

        var map = _transform.GetMap(location);
        if (!TryComp<MapGridComponent>(map, out var gridComp))
            return;

        var targetTile = TryGetReplaceTile(ent, GetCoordinates(args.Coordinates));

        if (targetTile is null)
            return;

        args.Handled = true;

        _map.SetTile((map.Value, gridComp), location, new Tile(_proto.Index(targetTile).TileId));
        _audio.PlayPredicted(ent.Comp.Sound, location, args.User);
    }
}
