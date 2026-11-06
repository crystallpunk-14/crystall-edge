using System.Numerics;
using Content.Shared._CE.Footprints;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Footprints;

// Content.Server._CE.Solution namespace shadows the type otherwise.
using Solution = Content.Shared.Chemistry.Components.Solution;

/// <summary>
/// Mopping footprints with an absorbent.
/// </summary>
public sealed partial class CEFootprintSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;

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
}
