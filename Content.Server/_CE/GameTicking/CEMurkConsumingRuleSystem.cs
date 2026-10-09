using System.Diagnostics.CodeAnalysis;
using Content.Server._CE.GameTicking.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Murk;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Roundflow;
using Content.Shared._CE.Trade.MainQuest;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.GameTicking;

public sealed partial class CEMurkConsumingRuleSystem : GameRuleSystem<CEMurkConsumingRuleComponent>
{
    [Dependency] private RoundEndSystem _roundEndSystem = default!;
    [Dependency] private CESharedMurkSystem _murk = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly EntProtoId _sphereShockwave = "CEShockWaveHugeVFX";

    /// <summary>
    /// Used for "days left" texts if no station map with a light cycle can be found.
    /// </summary>
    private static readonly TimeSpan FallbackDayDuration = TimeSpan.FromMinutes(60.0 / 7);

    protected override void Started(EntityUid uid, CEMurkConsumingRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        RollPrices(component);
    }

    /// <summary>
    /// Picks the round's prices from the pool: one per department first, then other entries of
    /// departments already used, and repeats an entry only if the pool is smaller than the price count.
    /// </summary>
    private void RollPrices(CEMurkConsumingRuleComponent component)
    {
        component.Prices.Clear();

        var pool = new List<CEMainQuestPricePrototype>(_proto.EnumeratePrototypes<CEMainQuestPricePrototype>());
        if (pool.Count == 0)
        {
            Log.Error("No mainQuestPrice prototypes - quest postaments will stay empty.");
            return;
        }

        RobustRandom.Shuffle(pool);

        var departments = new HashSet<string>();
        foreach (var price in pool)
        {
            if (component.Prices.Count >= component.PriceCount)
                break;

            if (departments.Add(price.Department))
                component.Prices.Add(price.ID);
        }

        foreach (var price in pool)
        {
            if (component.Prices.Count >= component.PriceCount)
                break;

            if (!component.Prices.Contains(price.ID))
                component.Prices.Add(price.ID);
        }

        while (component.Prices.Count < component.PriceCount)
        {
            component.Prices.Add(RobustRandom.Pick(pool).ID);
        }
    }

    /// <summary>
    /// The round's price number <paramref name="index"/> (1-based), if an active rule has rolled it.
    /// </summary>
    public bool TryGetPrice(int index, [NotNullWhen(true)] out CEMainQuestPricePrototype? price)
    {
        price = null;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var consuming, out _))
        {
            if (index < 1 || index > consuming.Prices.Count)
                return false;

            return _proto.Resolve(consuming.Prices[index - 1], out price);
        }

        return false;
    }

    public bool TryGetPriceCount(out int count)
    {
        count = 0;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var consuming, out _))
        {
            count = consuming.Prices.Count;
            return true;
        }

        return false;
    }

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
                case CEMurkSphereState.Cracked:
                    if (component.CrackTime is { } crackTime && Timing.CurTime >= crackTime + component.CollapseDelay)
                        Collapse();
                    break;
                case CEMurkSphereState.Collapsing:
                    DrainIntensity(sphereUid, source, component.CollapseRate * frameTime);
                    break;
            }
        }

        if (Timing.CurTime < component.NextBroadcast)
            return;

        component.NextBroadcast = Timing.CurTime + component.BroadcastInterval;
        BroadcastProgress(component);
    }

    private void StartRound(CEMurkConsumingRuleComponent component, Entity<CEMurkLusconSphereComponent> sphere)
    {
        component.CrackTime = Timing.CurTime;

        _murk.SetSphereState(sphere, CEMurkSphereState.Cracked);
        _appearance.SetData(sphere.Owner, CEMurkSphereState.Stable, sphere.Comp.State);
        Spawn(_sphereShockwave, Transform(sphere.Owner).Coordinates);

        RaiseNetworkEvent(new CEScreenPopupShowEvent(
            Loc.GetString("ce-murk-sphere-cracked-title"),
            Loc.GetString("ce-murk-sphere-cracked-desc", ("days", GetDaysLeft(component))),
            new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")));

        RaiseLocalEvent(new CERoundStartEvent());
    }

    private void Collapse()
    {
        var collapseQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
        while (collapseQuery.MoveNext(out var collapseUid, out var collapseSphere))
        {
            _murk.SetSphereState((collapseUid, collapseSphere), CEMurkSphereState.Collapsing);
            _appearance.SetData(collapseUid, CEMurkSphereState.Stable, collapseSphere.State);
            Spawn(_sphereShockwave, Transform(collapseUid).Coordinates);
        }

        _roundEndSystem.EndRound();
    }

    private void BroadcastProgress(CEMurkConsumingRuleComponent component)
    {
        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
        if (!sphereQuery.MoveNext(out _, out var sphere))
            return;

        // TODO: restoration ritual progress.
        var light = 0f;
        var murk = GetCollapseProgress(component);

        switch (sphere.State)
        {
            case CEMurkSphereState.Collapsing:
                murk = 1f;
                break;
            case CEMurkSphereState.Fixed:
                light = 1f;
                break;
        }

        RaiseNetworkEvent(new CERoundProgressStateEvent(
            sphere.State != CEMurkSphereState.Stable,
            Math.Clamp(light, 0f, 1f),
            murk));
    }

    /// <summary>
    /// How much of the time between the sphere cracking and collapsing has passed, 0..1.
    /// Stops advancing once the sphere leaves the cracked state.
    /// </summary>
    public float GetCollapseProgress(CEMurkConsumingRuleComponent component)
    {
        if (component.CrackTime is not { } crackTime)
            return 0f;

        if (component.CollapseDelay <= TimeSpan.Zero)
            return 1f;

        var now = component.CrackEndTime ?? Timing.CurTime;
        return Math.Clamp((float) ((now - crackTime) / component.CollapseDelay), 0f, 1f);
    }

    [SubscribeLocalEvent]
    private void OnSphereStateChanged(Entity<CEMurkLusconSphereComponent> ent, ref CEMurkSphereStateChangedEvent args)
    {
        if (args.OldState != CEMurkSphereState.Cracked)
            return;

        // Freeze the collapse countdown (e.g. the sphere was restored).
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var consuming, out _))
        {
            consuming.CrackEndTime ??= Timing.CurTime;
        }
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

            RaiseNetworkEvent(new CEScreenPopupShowEvent(
                Loc.GetString("ce-murk-days-left-title", ("days", GetDaysLeft(consuming))),
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
                Loc.GetString("ce-murk-sphere-cracked-desc", ("days", GetDaysLeft(consuming))),
                new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")), ev.Player);

            break;
        }
    }

    private bool IsSphereCracked()
    {
        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
        while (sphereQuery.MoveNext(out _, out var sphere))
        {
            return sphere.State == CEMurkSphereState.Cracked;
        }

        return false;
    }

    /// <summary>
    /// Remaining time until collapse, expressed in (rounded up) station day cycles.
    /// </summary>
    private int GetDaysLeft(CEMurkConsumingRuleComponent component)
    {
        var remaining = component.CollapseDelay * (1f - GetCollapseProgress(component));
        var day = GetDayDuration();
        if (day <= TimeSpan.Zero)
            return 0;

        return (int) Math.Ceiling(remaining / day - 0.001);
    }

    private TimeSpan GetDayDuration()
    {
        var query = EntityQueryEnumerator<LightCycleComponent, StationMemberComponent>();
        while (query.MoveNext(out var uid, out var lightCycle, out _))
        {
            if (TryComp<CEZMapComponent>(uid, out var zlevelMap) && zlevelMap.Depth != 0)
                continue;

            return lightCycle.Duration;
        }

        return FallbackDayDuration;
    }

    private void DrainIntensity(EntityUid uid, CEMurkSourceComponent source, float amount)
    {
        if (source.Intensity >= 0f)
            return;

        _murk.SetSourceIntensity((uid, source), MathF.Min(0f, source.Intensity + amount));
    }
}
