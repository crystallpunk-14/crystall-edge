using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Alchemy.Mortar;

public sealed partial class CEMortarSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDestructibleSystem _destructible = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedReagentGrinderSystem _grinder = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedStackSystem _stack = default!;

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<CEMortarComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<CEPestleComponent>(args.Used, out var pestle))
            return;

        args.Handled = true;

        if (!_container.TryGetContainer(ent, ent.Comp.Container, out var container)
            || !container.ContainedEntities.Any(item => GetGroundSolution(item) is not null))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, pestle.GrindDuration, new CEMortarGrindDoAfterEvent(), ent, ent, args.Used)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            // Extra clicks must not restart or cancel the grinding
            CancelDuplicate = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        // Server only: predicted audio cannot be stopped correctly on cancel
        if (_net.IsServer)
            pestle.AudioStream = _audio.PlayPvs(pestle.GrindSound, ent)?.Entity;
    }

    [SubscribeLocalEvent]
    private void OnGrindDoAfter(Entity<CEMortarComponent> ent, ref CEMortarGrindDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            if (TryComp<CEPestleComponent>(args.Used, out var pestle))
                pestle.AudioStream = _audio.Stop(pestle.AudioStream);
            return;
        }

        if (args.Handled)
            return;

        args.Handled = true;

        if (!_container.TryGetContainer(ent, ent.Comp.Container, out var container))
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var mortarSolution, out var solution))
            return;

        // What does not fit stays in the mortar
        foreach (var item in container.ContainedEntities.ToList())
        {
            if (GetGroundSolution(item) is not { } ground)
                continue;

            if (TryComp<StackComponent>(item, out var stack))
            {
                // Only the part of the stack that fits is ground. The tolerance covers float inaccuracy.
                var totalVolume = ground.Volume * stack.Count;
                var fitsCount = (int)(stack.Count * FixedPoint2.Min(solution.AvailableVolume / totalVolume + 0.01, 1));
                if (fitsCount <= 0)
                    continue;

                var scaled = new Solution(ground);
                scaled.ScaleSolution(fitsCount);

                if (!_solution.TryAddSolution(mortarSolution.Value, scaled))
                    continue;

                _stack.SetCount((item, stack), stack.Count - fitsCount);
            }
            else
            {
                if (ground.Volume > solution.AvailableVolume || !_solution.TryAddSolution(mortarSolution.Value, ground))
                    continue;

                _destructible.DestroyEntity(item);
            }
        }
    }

    /// <summary>
    /// What one item (or one unit of a stack) turns into. Items without grindable contents fall back to their juice.
    /// </summary>
    private Solution? GetGroundSolution(EntityUid item)
    {
        var solution = _grinder.GetGrinderSolution(item, GrinderProgram.Grind)
                       ?? _grinder.GetGrinderSolution(item, GrinderProgram.Juice);

        return solution is { Volume.Value: > 0 } ? solution : null;
    }
}

[Serializable, NetSerializable]
public sealed partial class CEMortarGrindDoAfterEvent : SimpleDoAfterEvent;
