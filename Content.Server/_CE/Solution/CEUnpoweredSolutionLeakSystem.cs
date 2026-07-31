using Content.Server._CE.Solution.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Power;
using Robust.Shared.Timing;

namespace Content.Server._CE.Solution;

public sealed partial class CEUnpoweredSolutionLeakSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEUnpoweredSolutionLeakComponent>();
        while (query.MoveNext(out var uid, out var leak))
        {
            if (_timing.CurTime < leak.NextLeakTime)
                continue;

            leak.NextLeakTime = _timing.CurTime + TimeSpan.FromSeconds(1);

            if (this.IsPowered(uid, EntityManager))
                continue;

            if (!_solutionContainer.ResolveSolution(uid, leak.SolutionName, ref leak.Solution, out var solution))
                continue;

            var amount = FixedPoint2.Min(leak.LeakRate, solution.Volume);
            if (amount <= FixedPoint2.Zero)
                continue;

            var spilled = _solutionContainer.SplitSolution(leak.Solution.Value, amount);
            _puddle.TrySpillAt(Transform(uid).Coordinates, spilled, out _, sound: false);
        }
    }
}
