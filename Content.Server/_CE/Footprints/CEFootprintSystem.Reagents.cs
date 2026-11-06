using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._CE.Footprints;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Footprints;

// Content.Server._CE.Solution namespace shadows the type otherwise.
using Solution = Content.Shared.Chemistry.Components.Solution;

/// <summary>
/// Reagent footprints: picking reagents up from puddles, leaving them as prints,
/// and merging prints with puddles.
/// </summary>
public sealed partial class CEFootprintSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private PuddleSystem _puddle = default!;

    [Dependency] private EntityQuery<PuddleComponent> _puddleQuery = default!;

    // Reusable buffer for footprints of one tile, cleared before every use.
    private readonly List<Entity<CEFootprintComponent>> _tileFootprints = new();

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

    /// <summary>
    /// Leaves a print from the carried reagents.
    /// </summary>
    /// <returns>False if nothing was carried, so no print was left.</returns>
    private bool LeavePrint(CEFootprintOwnerComponent owner,
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
            return false;

        // The more is carried, the bigger the print.
        var volume = FixedPoint2.Min(carried, (maxPrint - minPrint) * (carried / carrier.MaxVolume) + minPrint);
        if (volume < minPrint)
        {
            // Leftovers too small for a print would otherwise stick to the feet forever.
            carrier.Solution.RemoveAllSolution();
            return false;
        }

        var printSolution = carrier.Solution.SplitSolution(volume);
        var color = printSolution.GetColor(_proto).WithAlpha(owner.MaxAlpha * Math.Clamp((volume / maxPrint).Float(), 0f, 1f));

        if (!TrySpawnPrint(owner.FootprintProto, carrier.Sprite, color, grid, tile, position, rotation, out var print, out var tileCenter))
            return true;

        if (!_solution.TryGetSolution(print.Value.Owner, print.Value.Comp.SolutionName, out var printEnt))
        {
            Log.Error($"{ToPrettyString(print)} is missing its {print.Value.Comp.SolutionName} solution");
            Del(print);
            return true;
        }

        _solution.ForceAddSolution(printEnt.Value, printSolution);

        TryMergeIntoPuddle(grid, tile, tileCenter, print.Value.Comp.MaxTileVolume);
        return true;
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
}
