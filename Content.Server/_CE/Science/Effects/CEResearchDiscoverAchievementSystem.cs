using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science.Effects;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science.Effects;

public sealed class CEResearchDiscoverAchievementSystem : CEResearchActionEffectSystem<CEResearchDiscoverAchievement>
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private CEScienceSystem _science = default!;

    protected override void Effect(ref CEResearchActionEffectEvent<CEResearchDiscoverAchievement> args)
    {
        if (!_science.TryGetSingleton(out var science)
            || !science.Areas.TryGetValue(args.Args.Area, out var areaCells)
            || !areaCells.TryGetValue(args.Args.Coordinate, out var cell)
            || cell is not CEScienceAchievementCell achievementCell)
        {
            return;
        }

        if (!_proto.TryIndex(achievementCell.Achievement, out var achievement))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(args.Args.Actor);

        // Already discovered, or couldn't afford this achievement's own cost - the action's
        // generic Cost is 0 for this effect, so the real gating happens here.
        if (data.DiscoveredAchievements.Contains(achievementCell.Achievement))
            return;

        if (!_science.TrySpendPoints((args.Args.Actor, data), achievement.Cost))
            return;

        if (!_science.DiscoverAchievement((args.Args.Actor, data), achievementCell.Achievement))
            return;

        var effectArgs = new CEEntityEffectArgs(EntityManager, args.Args.Actor, null, default, 0f, args.Args.Actor, null);
        foreach (var effect in achievement.Effects)
        {
            effect.Effect(effectArgs);
        }
    }
}
