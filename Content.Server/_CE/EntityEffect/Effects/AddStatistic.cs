using Content.Server._CE.Statistics;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CEAddStatisticEffectSystem : CEEntityEffectSystem<AddStatistic>
{
    [Dependency] private CEStatisticSystem _statistic = default!;

    protected override void Effect(ref CEEntityEffectEvent<AddStatistic> args)
    {
        if (!CEStatistics.TryGetByName(args.Effect.Statistic, out var stat))
        {
            Log.Error($"Unknown statistic '{args.Effect.Statistic}' in {nameof(AddStatistic)} effect.");
            return;
        }

        if (args.Args.Resolve(args.Effect.EffectTarget) is not { } target)
            return;

        _statistic.TryAddStatistic(target, stat, args.Effect.Amount);
    }
}
