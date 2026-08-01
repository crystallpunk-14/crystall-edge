using Content.Server._CE.Solution.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._CE.Solution.Systems;

public sealed partial class CESolutionNormalizerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CESolutionNormalizerComponent>();
        while (query.MoveNext(out var uid, out var normalizer))
        {
            if (_timing.CurTime < normalizer.NextUpdateTime)
                continue;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            normalizer.NextUpdateTime = _timing.CurTime + normalizer.UpdateFrequency;

            if (!_solutionContainer.ResolveSolution(uid, normalizer.SolutionName, ref normalizer.Solution, out var solution))
                continue;

            if (solution.Volume == FixedPoint2.Zero)
                continue;

            var minQuantity = FixedPoint2.MaxValue;
            ReagentId? reagentId = null;
            foreach (var (id, quantity) in solution.Contents)
            {
                if (quantity < minQuantity)
                {
                    reagentId = id;
                    minQuantity = quantity;
                }
            }

            if (reagentId is not { } reagent)
                continue;

            _solutionContainer.RemoveReagent(normalizer.Solution.Value, reagent, normalizer.LeakageQuantity);
            _audio.PlayPvs(normalizer.NormalizeSound, uid);
        }
    }
}
