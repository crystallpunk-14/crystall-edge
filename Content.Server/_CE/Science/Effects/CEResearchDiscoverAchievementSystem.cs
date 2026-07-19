using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science.Effects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Science.Effects;

public sealed class CEResearchDiscoverAchievementSystem : CEResearchActionEffectSystem<CEResearchDiscoverAchievement>
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private CEScienceSystem _science = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier DiscoverySound =
        new SoundPathSpecifier(new ResPath("/Audio/_CE/Effects/knowledge_learned.ogg"));

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

        _science.TryDiscoverAchievement((args.Args.Actor, data), achievementCell.Achievement);

        _audio.PlayPvs(DiscoverySound, args.Args.Table);
    }
}
