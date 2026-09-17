using Content.Server._CE.GameTicking.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Murk.Systems;
using Content.Shared._CE.Roundflow;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GameTicking;

public sealed partial class CEMurkConsumingRuleSystem : GameRuleSystem<CEMurkConsumingRuleComponent>
{
    [Dependency] private RoundEndSystem _roundEndSystem = default!;
    [Dependency] private CESharedMurkSystem _murk = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private readonly EntProtoId _sphereShockwave = "CEShockWaveHugeVFX";

    protected override void ActiveTick(EntityUid uid, CEMurkConsumingRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent, CEMurkSourceComponent>();
        while (sphereQuery.MoveNext(out var sphereUid, out var sphere, out var source))
        {
            switch (sphere.State)
            {
                case CEMurkSphereState.Stable:
                    if (Timing.CurTime >= GameTicker.RoundStartTimeSpan + sphere.CrackDelay)
                        StartRound((sphereUid, sphere));
                    break;
                case CEMurkSphereState.Collapsing:
                    DrainIntensity(sphereUid, source, sphere.CollapseRate * frameTime);
                    break;
            }
        }
    }

    private void StartRound(Entity<CEMurkLusconSphereComponent> sphere)
    {
        sphere.Comp.State = CEMurkSphereState.Cracked;
        Dirty(sphere);
        _appearance.SetData(sphere.Owner, CEMurkSphereState.Stable, sphere.Comp.State);
        Spawn(_sphereShockwave, Transform(sphere.Owner).Coordinates);

        RaiseNetworkEvent(new CEScreenPopupShowEvent(
            Loc.GetString("ce-murk-sphere-cracked-title"),
            Loc.GetString("ce-murk-sphere-cracked-desc", ("days", sphere.Comp.DaysToCollapse)),
            new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")));

        RaiseLocalEvent(new CERoundStartEvent());
    }

    protected override void AppendRoundEndText(EntityUid uid,
        CEMurkConsumingRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        args.AddLine("TODO");
    }

    [SubscribeLocalEvent]
    private void OnStartDay(CEStartDayEvent ev)
    {
        if (TryComp<CEZMapComponent>(ev.MapUid, out var zlevelMap) && zlevelMap.Depth != 0)
            return; //We don't care about zlevels start day event

        if (!HasComp<StationMemberComponent>(ev.MapUid))
            return;

        var query = QueryActiveRules();
        if (!query.MoveNext(out _, out _, out _, out _))
            return; //No active rule, sphere shouldn't be counting down

        //Days only count down after the sphere has cracked, and stop once it's collapsing
        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent, CEMurkSourceComponent>();
        while (sphereQuery.MoveNext(out var sphereUid, out var sphere, out var source))
        {
            if (sphere.State != CEMurkSphereState.Cracked)
                continue;

            DrainIntensity(sphereUid, source, sphere.IntensityPerDay);

            sphere.DaysSinceCrack++;
            Dirty(sphereUid, sphere);

            if (sphere.DaysSinceCrack >= sphere.DaysToCollapse)
            {
                sphere.State = CEMurkSphereState.Collapsing;
                _appearance.SetData(sphereUid, CEMurkSphereState.Stable, sphere.State);
                Spawn(_sphereShockwave, Transform(sphereUid).Coordinates);
                _roundEndSystem.EndRound();
            }

            RaiseNetworkEvent(new CEScreenPopupShowEvent(
                Loc.GetString("ce-murk-days-left-title", ("days", sphere.DaysToCollapse - sphere.DaysSinceCrack)),
                "",
                new SoundPathSpecifier("/Audio/_CE/Announce/event_boom.ogg")));
        }
    }

    [SubscribeLocalEvent]
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin)
            return;

        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
        while (sphereQuery.MoveNext(out _, out var sphere))
        {
            if (sphere.State == CEMurkSphereState.Stable)
                return; //Sphere hasn't cracked yet, nothing to tell this player yet

            RaiseNetworkEvent(new CEScreenPopupShowEvent(
                Loc.GetString("ce-murk-sphere-cracked-title"),
                Loc.GetString("ce-murk-sphere-cracked-desc", ("days", sphere.DaysToCollapse - sphere.DaysSinceCrack)),
                new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")), ev.Player);

            break;
        }
    }

    private void DrainIntensity(EntityUid uid, CEMurkSourceComponent source, float amount)
    {
        if (source.Intensity >= 0f)
            return;

        _murk.SetSourceIntensity((uid, source), MathF.Min(0f, source.Intensity + amount));
    }
}
