using Content.Server._CE.GameTicking.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Roundflow;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles.Jobs;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;

namespace Content.Server._CE.GameTicking;

/// <summary>
/// TEMP SHITCODE PROTOTYPE SYSTEM. Unlocalized strings is ok here. We rewrite it in future
/// </summary>
public sealed class CELimitedDaysRuleSystem : GameRuleSystem<CELimitedDaysRuleComponent>
{
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<CEStartDayEvent>(OnStartDay);
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        //When a player spawn in, we immediately tell them what day it is today.
        var query = EntityQueryEnumerator<CELimitedDaysRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var limitedDays, out var rule))
        {
            RaiseNetworkEvent(new CEScreenPopupShowEvent($"Welcome to {MetaData(ev.Station).EntityName}",
                $"An [color=red]unknown threat[/color] lurks in the city",
                new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg", new AudioParams(){Pitch = 1.0f})),
                ev.Player);
            RaiseNetworkEvent(new CEScreenPopupShowEvent($"Days left: {limitedDays.Max - limitedDays.Current}",
                $"Survive and achieve your goals",
                new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg", new AudioParams(){Pitch = 0.7f})),
                ev.Player);

            break; //Yeea we dont have multiple rules support rn.
        }
    }

    protected override void AppendRoundEndText(EntityUid uid,
        CELimitedDaysRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        args.AddLine($"[head=2][color=#75c8ff]Results of the round[/color][/head]");
        var alivePercentage = CalculateAlivePlayersPercentage();
        args.AddLine($"Alive Players Percentage: {alivePercentage * 100f:0.0}%");
    }

    private void OnStartDay(CEStartDayEvent ev)
    {
        if (TryComp<CEZLevelMapComponent>(ev.MapUid, out var zlevelMap) && zlevelMap.Depth != 0)
            return; //We don't care about zlevels start day event

        if (!HasComp<StationMemberComponent>(ev.MapUid))
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var limitedDays, out _))
        {
            limitedDays.Current++;

            if (limitedDays.Current >= limitedDays.Max)
            {
                _roundEndSystem.EndRound();
            }
            RaiseNetworkEvent(new CEScreenPopupShowEvent($"Days left: {limitedDays.Max - limitedDays.Current}",
                $"",
                new SoundPathSpecifier("/Audio/_CE/Announce/event_boom.ogg")));

            break;
        }
    }

    public float CalculateAlivePlayersPercentage()
    {
        var query = EntityQueryEnumerator<StationJobsComponent>();

        var totalPlayers = 0f;
        var alivePlayers = 0f;
        while (query.MoveNext(out var uid, out var jobs))
        {
            totalPlayers += jobs.PlayerJobs.Count;

            foreach (var (netUserId, jobsList) in jobs.PlayerJobs)
            {
                if (!_mind.TryGetMind(netUserId, out var mind))
                    continue;

                if (!_jobs.MindTryGetJob(mind, out var jobRole))
                    continue;

                var firstMindEntity = GetEntity(mind.Value.Comp.OriginalOwnedEntity);

                if (firstMindEntity is null)
                    continue;

                if (!_mobState.IsDead(firstMindEntity.Value))
                    alivePlayers++;
            }
        }

        return totalPlayers > 0 ? (alivePlayers / totalPlayers) : 0f;
    }

    private string GetDayOfWeek(int day)
    {
        return day switch
        {
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            6 => "Saturday",
            7 => "Sunday",
            _ => throw new ArgumentOutOfRangeException(nameof(day), "Day must be between 1 and 7")
        };
    }
}
