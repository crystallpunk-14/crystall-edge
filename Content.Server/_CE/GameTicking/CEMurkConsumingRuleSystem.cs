using Content.Server._CE.GameTicking.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Murk;
using Content.Shared._CE.Murk.Components;
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
                    if (Timing.CurTime >= GameTicker.RoundStartTimeSpan + component.CrackDelay)
                        StartRound(component, (sphereUid, sphere));
                    break;
                case CEMurkSphereState.Collapsing:
                    DrainIntensity(sphereUid, source, component.CollapseRate * frameTime);
                    break;
            }
        }
    }

    private void StartRound(CEMurkConsumingRuleComponent component, Entity<CEMurkLusconSphereComponent> sphere)
    {
        sphere.Comp.State = CEMurkSphereState.Cracked;
        Dirty(sphere);
        _appearance.SetData(sphere.Owner, CEMurkSphereState.Stable, sphere.Comp.State);
        Spawn(_sphereShockwave, Transform(sphere.Owner).Coordinates);

        RaiseNetworkEvent(new CEScreenPopupShowEvent(
            Loc.GetString("ce-murk-sphere-cracked-title"),
            Loc.GetString("ce-murk-sphere-cracked-desc", ("days", component.DaysToCollapse)),
            new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")));

        RaiseLocalEvent(new CERoundStartEvent());
    }

    [SubscribeLocalEvent]
    private void OnStartDay(CEStartDayEvent ev)
    {
        if (TryComp<CEZMapComponent>(ev.MapUid, out var zlevelMap) && zlevelMap.Depth != 0)
            return; //We don't care about zlevels start day event

        if (!HasComp<StationMemberComponent>(ev.MapUid))
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var consuming, out _))
        {
            //Days only count down after the sphere has cracked, and stop once it's collapsing
            if (!IsSphereCracked())
                break;

            var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent, CEMurkSourceComponent>();
            while (sphereQuery.MoveNext(out var sphereUid, out var sphere, out var source))
            {
                if (sphere.State != CEMurkSphereState.Cracked)
                    continue;

                DrainIntensity(sphereUid, source, consuming.IntensityPerDay);
            }

            consuming.DaysSinceCrack++;

            if (consuming.DaysSinceCrack >= consuming.DaysToCollapse)
            {
                var collapseQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
                while (collapseQuery.MoveNext(out var collapseUid, out var collapseSphere))
                {
                    collapseSphere.State = CEMurkSphereState.Collapsing;
                    Dirty(collapseUid, collapseSphere);
                    _appearance.SetData(collapseUid, CEMurkSphereState.Stable, collapseSphere.State);
                    Spawn(_sphereShockwave, Transform(collapseUid).Coordinates);
                }

                _roundEndSystem.EndRound();
            }

            RaiseNetworkEvent(new CEScreenPopupShowEvent(
                Loc.GetString("ce-murk-days-left-title", ("days", consuming.DaysToCollapse - consuming.DaysSinceCrack)),
                "",
                new SoundPathSpecifier("/Audio/_CE/Announce/event_boom.ogg")));

            break; //Yeea we dont have multiple rules support rn.
        }
    }

    [SubscribeLocalEvent]
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin)
            return;

        if (!IsSphereCracked())
            return; //Sphere hasn't cracked yet, nothing to tell this player yet

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var consuming, out _))
        {
            RaiseNetworkEvent(new CEScreenPopupShowEvent(
                Loc.GetString("ce-murk-sphere-cracked-title"),
                Loc.GetString("ce-murk-sphere-cracked-desc", ("days", consuming.DaysToCollapse - consuming.DaysSinceCrack)),
                new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")), ev.Player);

            break;
        }
    }

    private bool IsSphereCracked()
    {
        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
        while (sphereQuery.MoveNext(out _, out var sphere))
        {
            return sphere.State != CEMurkSphereState.Stable;
        }

        return false;
    }

    private void DrainIntensity(EntityUid uid, CEMurkSourceComponent source, float amount)
    {
        if (source.Intensity >= 0f)
            return;

        _murk.SetSourceIntensity((uid, source), MathF.Min(0f, source.Intensity + amount));
    }
}
