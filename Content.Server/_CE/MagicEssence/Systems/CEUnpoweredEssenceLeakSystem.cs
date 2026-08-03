using Content.Server._CE.MagicEssence.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Power;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.MagicEssence.Systems;

public sealed partial class CEUnpoweredEssenceLeakSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private CEMagicEssenceSystem _essence = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEUnpoweredEssenceLeakComponent>();
        while (query.MoveNext(out var uid, out var leak))
        {
            if (_timing.CurTime < leak.NextLeakTime)
                continue;

            leak.NextLeakTime = _timing.CurTime + TimeSpan.FromSeconds(1);

            if (this.IsPowered(uid, EntityManager))
                continue;

            if (!_solutionContainer.ResolveSolution(uid, leak.SolutionName, ref leak.Solution, out var solution))
                continue;

            if (solution.Contents.Count == 0)
                continue;

            var picked = solution.Contents[_random.Next(solution.Contents.Count)];

            if (_essence.TryGetEssenceFromReagent(picked.Reagent.Prototype, out var essenceProto))
            {
                var evaporated = _solutionContainer.RemoveReagent(leak.Solution.Value, picked.Reagent, FixedPoint2.New(1));
                if (evaporated > FixedPoint2.Zero)
                    Spawn(essenceProto, Transform(uid).Coordinates);

                continue;
            }

            var amount = FixedPoint2.Min(leak.LeakRate, solution.Volume);
            if (amount <= FixedPoint2.Zero)
                continue;

            var spilled = _solutionContainer.SplitSolution(leak.Solution.Value, amount);
            _puddle.TrySpillAt(Transform(uid).Coordinates, spilled, out _, sound: false);
        }
    }
}
