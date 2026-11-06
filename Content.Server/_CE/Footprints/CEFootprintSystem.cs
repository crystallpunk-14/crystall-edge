using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._CE.Footprints;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Standing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Footprints;

// Content.Server._CE.Solution namespace shadows the type otherwise.
using Solution = Content.Shared.Chemistry.Components.Solution;

/// <summary>
/// Moving entities leave footprints: reagent prints from what they carry on feet or body
/// (see <c>CEFootprintSystem.Reagents.cs</c>), or temporary prints on soft tiles (see <c>CEFootprintSystem.Tiles.cs</c>).
/// </summary>
public sealed partial class CEFootprintSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private InventorySystem _inventory = default!;

    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<CEFootprintComponent> _footprintQuery = default!;
    [Dependency] private EntityQuery<CEFootprintShoesComponent> _shoesQuery = default!;
    [Dependency] private EntityQuery<CEZPhysicsComponent> _zPhysicsQuery = default!;

    /// <summary>
    /// Movement longer than this in a single move is a teleport, not a step.
    /// </summary>
    private const float MaxStepLength = 2f;

    [SubscribeLocalEvent]
    private void OnMove(Entity<CEFootprintOwnerComponent> ent, ref MoveEvent args)
    {
        if (args.OnlyRotation)
            return;

        if (args.ParentChanged)
        {
            ent.Comp.Distance = 0;
            return;
        }

        var xform = args.Component;
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return;

        // Usually the parent is the grid itself and no conversion is needed.
        var oldPos = args.OldPosition.EntityId == gridUid
            ? args.OldPosition.Position
            : _transform.WithEntityId(args.OldPosition, gridUid).Position;
        var newPos = args.NewPosition.EntityId == gridUid
            ? args.NewPosition.Position
            : _transform.WithEntityId(args.NewPosition, gridUid).Position;
        var delta = newPos - oldPos;
        var length = delta.Length();

        if (length > MaxStepLength)
        {
            ent.Comp.Distance = 0;
            return;
        }

        if (_zPhysicsQuery.TryComp(ent, out var zPhysics) && zPhysics.LocalPosition > 0)
            return;

        if (_gravity.IsWeightless(ent.Owner))
            return;

        var lying = _standing.IsDown(ent.Owner);

        ent.Comp.Distance += length;
        if (ent.Comp.Distance < (lying ? ent.Comp.BodyDistance : ent.Comp.FootDistance))
            return;

        ent.Comp.Distance = 0;

        if (!TryGetCarrier(ent, lying, out var carrier))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, args.NewPosition);

        if (TryGetPuddle((gridUid, grid), tile, out var puddle))
        {
            PickUp(carrier.Value, ent.Comp.PickupVolume, puddle.Value);
            return;
        }

        var rotation = lying
            ? delta.ToAngle()
            : _transform.GetWorldRotation(xform) - _transform.GetWorldRotation(gridUid);

        // Reagent prints take priority, soft tiles only get a print from clean feet.
        if (LeavePrint(ent.Comp, carrier.Value, lying, (gridUid, grid), tile, newPos, rotation))
            return;

        LeaveTilePrint(ent.Comp, carrier.Value, (gridUid, grid), tile, newPos, rotation);
    }

    private bool TryGetCarrier(Entity<CEFootprintOwnerComponent> ent, bool lying, [NotNullWhen(true)] out Carrier? carrier)
    {
        carrier = null;

        if (lying)
        {
            carrier = new Carrier(ent.Comp.BodySolution, ent.Comp.MaxBodyVolume, ent.Comp.BodySprite);
            return true;
        }

        var shoesEnumerator = _inventory.GetSlotEnumerator(ent.Owner, SlotFlags.FEET);
        while (shoesEnumerator.NextItem(out var item))
        {
            // Shoes that don't support footprints keep the feet clean and leave nothing.
            if (!_shoesQuery.TryComp(item, out var shoes))
                return false;

            carrier = new Carrier(shoes.Solution, shoes.MaxVolume, shoes.Sprite);
            return true;
        }

        carrier = new Carrier(ent.Comp.FeetSolution, ent.Comp.MaxFeetVolume, ent.Comp.FootSprite);
        return true;
    }

    private bool TrySpawnPrint(EntProtoId proto,
        SpriteSpecifier sprite,
        Color color,
        Entity<MapGridComponent> grid,
        Vector2i tile,
        Vector2 position,
        Angle rotation,
        [NotNullWhen(true)] out Entity<CEFootprintComponent>? print,
        out EntityCoordinates tileCenter)
    {
        print = null;
        tileCenter = _map.GridTileToLocal(grid, grid, tile);

        var uid = Spawn(proto, tileCenter);
        if (!_footprintQuery.TryComp(uid, out var footprint))
        {
            Log.Error($"{ToPrettyString(uid)} is missing {nameof(CEFootprintComponent)}");
            Del(uid);
            return false;
        }

        footprint.Sprite = sprite;
        footprint.Color = color;
        footprint.Offset = position - tileCenter.Position;
        footprint.Rotation = rotation;
        Dirty(uid, footprint);

        print = (uid, footprint);
        return true;
    }

    /// <summary>
    /// Where the feet or body reagents are kept and which print sprite they leave.
    /// </summary>
    private readonly record struct Carrier(Solution Solution, FixedPoint2 MaxVolume, SpriteSpecifier Sprite);
}
