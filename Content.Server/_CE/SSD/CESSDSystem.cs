using Content.Server._CE.Achievements;
using Content.Server._CE.GameTicking;
using Content.Server._CE.Statistics;
using Content.Shared.CCVar;
using Content.Shared.Mobs.Systems;
using Content.Shared.SSDIndicator;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._CE.SSD;

/// <summary>
/// Players whose main character stays alive and SSD for longer than <see cref="CCVars.CEAchievementsSSDThreshold"/>
/// earn no achievements or statistics.
/// </summary>
public sealed partial class CESSDSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private CEMainCharacterSystem _mainCharacter = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [SubscribeLocalEvent]
    private void OnAchievementAttempt(ref CEAchievementAttemptEvent args)
    {
        if (IsLongSSD(args.Player))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnStatisticAttempt(ref CEStatisticAttemptEvent args)
    {
        if (IsLongSSD(args.Player))
            args.Cancelled = true;
    }

    private bool IsLongSSD(NetUserId player)
    {
        if (!_mainCharacter.TryGetMainCharacter(player, out var character) ||
            !TryComp<SSDIndicatorComponent>(character, out var ssd) ||
            !ssd.IsSSD ||
            _mobState.IsDead(character.Value))
            return false;

        var threshold = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.CEAchievementsSSDThreshold));
        return _timing.CurTime - ssd.SSDStartTime >= threshold;
    }
}
