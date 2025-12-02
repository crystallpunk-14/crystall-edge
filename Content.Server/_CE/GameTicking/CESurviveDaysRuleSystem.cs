using Content.Server._CE.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Roundflow;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;

namespace Content.Server._CE.GameTicking;

/// <summary>
///
/// </summary>
public sealed class CESurviveDaysRuleSystem : GameRuleSystem<CESurviveDaysRuleComponent>
{
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEStartDayEvent>(OnStartDay);
    }

    private void OnStartDay(CEStartDayEvent ev)
    {
        if (TryComp<CEZLevelMapComponent>(ev.MapUid, out var zlevelMap) && zlevelMap.Depth != 0)
            return; //We don't care about zlevels start day event

        if (!TryComp<StationMemberComponent>(ev.MapUid, out var stationComp))
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var survive, out _))
        {
            survive.DaysSurvived++;

            if (survive.DaysSurvived > 7)
            {
                _roundEndSystem.EndRound();
                RaiseNetworkEvent(new CEScreenPopupShowEvent($"End of week", $"Guys?", new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")));
            }
            else
            {
                RaiseNetworkEvent(new CEScreenPopupShowEvent($"{GetDayOfWeek(survive.DaysSurvived)}", $"Day ({survive.DaysSurvived}/7)", new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")));
            }
            break;
        }
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
