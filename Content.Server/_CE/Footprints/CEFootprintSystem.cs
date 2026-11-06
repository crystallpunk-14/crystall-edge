using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._CE.Footprints;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Gravity;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Footprints;

// Content.Server._CE.Solution namespace shadows the type otherwise.
using Solution = Content.Shared.Chemistry.Components.Solution;

/// <summary>
/// Moving entities pick up reagents from puddles and leave them on the floor as footprint entities.
/// Footprints on one tile merge into a puddle once their total volume reaches the limit.
/// </summary>
public sealed partial class CEFootprintSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;

    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<CEFootprintComponent> _footprintQuery = default!;
    [Dependency] private EntityQuery<CEFootprintShoesComponent> _shoesQuery = default!;
    [Dependency] private EntityQuery<PuddleComponent> _puddleQuery = default!;
    [Dependency] private EntityQuery<CEZPhysicsComponent> _zPhysicsQuery = default!;

    /// <summary>
    /// Movement longer than this in a single move is a teleport, not a step.
    /// </summary>
    private const float MaxStepLength = 2f;

    // Reusable buffer for footprints of one tile, cleared before every use.
    private readonly List<Entity<CEFootprintComponent>> _tileFootprints = new();

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

        LeavePrint(ent.Comp, carrier.Value, lying, (gridUid, grid), tile, newPos, rotation);
    }

    /// <summary>
    /// Merges footprints into a puddle that appears on their tile.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPuddleMapInit(Entity<PuddleComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        if (!CollectFootprints((gridUid, grid), tile))
            return;

        var merged = DeleteFootprints();
        if (merged.Volume > FixedPoint2.Zero)
            _puddle.TryAddSolution(ent, merged, sound: false, puddleComponent: ent.Comp);
    }

    /// <summary>
    /// Mopping a footprint wipes all footprints on its tile into the absorbent.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<CEFootprintComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<AbsorbentComponent>(args.Used, out var absorbent))
            return;

        args.Handled = true;

        if (TryComp<UseDelayComponent>(args.Used, out var useDelay) && _useDelay.IsDelayed((args.Used, useDelay)))
            return;

        if (!_solution.TryGetSolution(args.Used, absorbent.SolutionName, out var absorberEnt, out var absorberSolution))
            return;

        FixedPoint2 budget;
        ProtoId<ReagentPrototype>[] absorbentReagents = [];
        if (absorbent.UseAbsorberSolution)
        {
            absorbentReagents = _puddle.GetAbsorbentReagents(absorberSolution);
            var available = absorberSolution.GetTotalPrototypeQuantity(absorbentReagents);
            if (available == FixedPoint2.Zero)
            {
                _popup.PopupEntity(Loc.GetString("mopping-system-no-water", ("used", args.Used)), args.Used, args.User);
                return;
            }

            budget = FixedPoint2.Min(absorbent.PickupAmount, available);
        }
        else
        {
            budget = FixedPoint2.Min(absorbent.PickupAmount, absorberSolution.AvailableVolume);
        }

        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        if (!CollectFootprints((gridUid, grid), tile))
            return;

        var picked = new Solution();
        foreach (var footprint in _tileFootprints)
        {
            if (budget <= FixedPoint2.Zero)
                break;

            if (!_solution.TryGetSolution(footprint.Owner, footprint.Comp.SolutionName, out var printEnt, out var printSolution))
                continue;

            var taken = printSolution.SplitSolution(budget);
            budget -= taken.Volume;
            picked.AddSolution(taken, _proto);

            if (printSolution.Volume <= FixedPoint2.Zero)
                Del(footprint);
            else
                _solution.UpdateChemicals(printEnt.Value, false);
        }

        if (picked.Volume <= FixedPoint2.Zero)
            return;

        // The water spent on wiping stays on the floor and dries out.
        if (absorbent.UseAbsorberSolution)
            absorberSolution.SplitSolutionWithOnly(picked.Volume, absorbentReagents);

        _solution.AddSolution(absorberEnt.Value, picked);

        // Same feedback as mopping a puddle in SharedAbsorbentSystem.
        var coords = _map.GridTileToLocal(gridUid, grid, tile);
        _audio.PlayPvs(absorbent.PickupSound, coords);

        if (_tileFootprints.TrueForAll(footprint => TerminatingOrDeleted(footprint)))
            Spawn(absorbent.MoppedEffect, coords);

        if (useDelay != null)
            _useDelay.TryResetDelay((args.Used, useDelay));

        var userXform = Transform(args.User);
        // The clicked footprint may be deleted already, so aim at its tile.
        var localPos = Vector2.Transform(_transform.ToMapCoordinates(coords).Position, _transform.GetInvWorldMatrix(userXform));
        localPos = userXform.LocalRotation.RotateVec(localPos);

        // The handler runs only on the server, so the user doesn't predict the lunge and needs it sent too.
        _melee.DoLunge(args.User, args.Used, Angle.Zero, localPos, null, predicted: false);
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

    private void PickUp(Carrier carrier, FixedPoint2 pickupVolume, Entity<PuddleComponent> puddle)
    {
        if (!_solution.ResolveSolution(puddle.Owner, puddle.Comp.SolutionName, ref puddle.Comp.Solution, out var puddleSolution))
            return;

        var space = FixedPoint2.Min(carrier.MaxVolume - carrier.Solution.Volume, pickupVolume);
        if (space <= FixedPoint2.Zero)
            return;

        var taken = puddleSolution.SplitSolutionWithout(space, _puddle.GetEvaporatingReagents(puddleSolution));
        if (taken.Volume <= FixedPoint2.Zero)
            return;

        carrier.Solution.AddSolution(taken, _proto);
        _solution.UpdateChemicals(puddle.Comp.Solution.Value, false);
    }

    private void LeavePrint(CEFootprintOwnerComponent owner,
        Carrier carrier,
        bool lying,
        Entity<MapGridComponent> grid,
        Vector2i tile,
        Vector2 position,
        Angle rotation)
    {
        var minPrint = lying ? owner.MinBodyprintVolume : owner.MinFootprintVolume;
        var maxPrint = lying ? owner.MaxBodyprintVolume : owner.MaxFootprintVolume;

        var carried = carrier.Solution.Volume;
        if (carried <= FixedPoint2.Zero)
            return;

        // The more is carried, the bigger the print.
        var volume = FixedPoint2.Min(carried, (maxPrint - minPrint) * (carried / carrier.MaxVolume) + minPrint);
        if (volume < minPrint)
        {
            // Leftovers too small for a print would otherwise stick to the feet forever.
            carrier.Solution.RemoveAllSolution();
            return;
        }

        var printSolution = carrier.Solution.SplitSolution(volume);

        var tileCenter = _map.GridTileToLocal(grid, grid, tile);
        var print = Spawn(owner.FootprintProto, tileCenter);
        if (!_footprintQuery.TryComp(print, out var footprint)
            || !_solution.TryGetSolution(print, footprint.SolutionName, out var printEnt))
        {
            Log.Error($"{ToPrettyString(print)} is missing {nameof(CEFootprintComponent)} or its solution");
            Del(print);
            return;
        }

        footprint.Sprite = carrier.Sprite;
        footprint.Color = printSolution.GetColor(_proto).WithAlpha(owner.MaxAlpha * Math.Clamp((volume / maxPrint).Float(), 0f, 1f));
        footprint.Offset = position - tileCenter.Position;
        footprint.Rotation = rotation;
        Dirty(print, footprint);

        _solution.ForceAddSolution(printEnt.Value, printSolution);

        TryMergeIntoPuddle(grid, tile, tileCenter, footprint.MaxTileVolume);
    }

    private void TryMergeIntoPuddle(Entity<MapGridComponent> grid, Vector2i tile, EntityCoordinates tileCenter, FixedPoint2 maxTileVolume)
    {
        if (!CollectFootprints(grid, tile))
            return;

        var total = FixedPoint2.Zero;
        foreach (var footprint in _tileFootprints)
        {
            if (_solution.TryGetSolution(footprint.Owner, footprint.Comp.SolutionName, out _, out var solution))
                total += solution.Volume;
        }

        if (total < maxTileVolume)
            return;

        var merged = DeleteFootprints();
        _puddle.TrySpillAt(tileCenter, merged, out _, sound: false);
    }

    /// <summary>
    /// Fills <see cref="_tileFootprints"/> with the footprints anchored on the tile.
    /// </summary>
    private bool CollectFootprints(Entity<MapGridComponent> grid, Vector2i tile)
    {
        _tileFootprints.Clear();

        var anchored = _map.GetAnchoredEntitiesEnumerator(grid, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (_footprintQuery.TryComp(uid, out var footprint))
                _tileFootprints.Add((uid.Value, footprint));
        }

        return _tileFootprints.Count > 0;
    }

    /// <summary>
    /// Deletes everything in <see cref="_tileFootprints"/> and returns their reagents.
    /// </summary>
    private Solution DeleteFootprints()
    {
        var merged = new Solution();
        foreach (var footprint in _tileFootprints)
        {
            if (_solution.TryGetSolution(footprint.Owner, footprint.Comp.SolutionName, out _, out var solution))
                merged.AddSolution(solution, _proto);

            Del(footprint);
        }

        _tileFootprints.Clear();
        return merged;
    }

    private bool TryGetPuddle(Entity<MapGridComponent> grid, Vector2i tile, [NotNullWhen(true)] out Entity<PuddleComponent>? puddle)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(grid, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (_puddleQuery.TryComp(uid, out var comp))
            {
                puddle = (uid.Value, comp);
                return true;
            }
        }

        puddle = null;
        return false;
    }

    private readonly record struct Carrier(Solution Solution, FixedPoint2 MaxVolume, SpriteSpecifier Sprite);
}
