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

            if (survive.DaysSurvived >= 7)
            {
                _roundEndSystem.EndRound();
            }
            else
            {
                RaiseNetworkEvent(new CEScreenPopupShowEvent($"День {survive.DaysSurvived}", "охуеть", new SoundPathSpecifier("/Audio/Animals/bear.ogg")));
            }
        }
    }
}
