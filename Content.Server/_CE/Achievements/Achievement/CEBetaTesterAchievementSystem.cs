using Content.Shared._CE.Achievements.Prototypes;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Achievements.Achievement;

public sealed partial class CEBetaTesterAchievementSystem : EntitySystem
{
    [Dependency] private CEAchievementsSystem _achievement = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private readonly ProtoId<CEAchievementPrototype> _proto = "BetaTester";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
    }

    private async void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        // Don't award achievements in integration tests to avoid interfering with test cleanup
        if (_cfg.GetCVar(CCVars.DatabaseSynchronous))
            return;

        await _achievement.AddPlayerAchievementAsync(ev.Player.UserId, _proto);
    }
}
