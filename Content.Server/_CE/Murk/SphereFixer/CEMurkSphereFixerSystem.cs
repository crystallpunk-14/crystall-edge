using Content.Server.Power.Components;
using Content.Server.RoundEnd;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Roundflow;
using Content.Shared.Power;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._CE.Murk.SphereFixer;

/// <summary>
/// Drives the Pillar of Light: accumulates charge while nothing blocks it, and once full,
/// mends every cracked Lucson Sphere and ends the round.
/// </summary>
public sealed partial class CEMurkSphereFixerSystem : EntitySystem
{
    [Dependency] private RoundEndSystem _roundEndSystem = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEMurkSphereFixerComponent> ent, ref MapInitEvent args)
    {
        RefreshBlockConditions();
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CEMurkSphereFixerComponent> ent, ref PowerChangedEvent args)
    {
        RefreshBlockConditions();
    }

    /// <summary>
    /// The first charge condition: the Pillar itself must be powered. Modeled after CEDrill's
    /// power gating, but pull-based since it only runs when <see cref="RefreshBlockConditions"/>
    /// is called instead of caching its own state off a separate subscription.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPowerBlockCheck(Entity<CEMurkSphereFixerComponent> ent, ref CEMurkSphereFixerBlockRefreshEvent args)
    {
        if (TryComp<ApcPowerReceiverComponent>(ent, out var power) && !power.Powered)
        {
            args.Block(Loc.GetString("ce-murk-sphere-fixer-block-unpowered-title"),
                Loc.GetString("ce-murk-sphere-fixer-block-unpowered-desc"),
                Transform(ent).Coordinates);
        }
    }

    /// <summary>
    /// Recomputes and caches whether charging is currently blocked, for whichever Pillar of Light
    /// exists (there's normally only one per round). Call this whenever something a condition
    /// depends on changes - not every tick. Callers don't need to resolve the Pillar's entity
    /// themselves.
    /// </summary>
    public void RefreshBlockConditions()
    {
        var query = EntityQueryEnumerator<CEMurkSphereFixerComponent>();
        if (!query.MoveNext(out var uid, out var fixer))
            return;

        var ev = new CEMurkSphereFixerBlockRefreshEvent();
        RaiseLocalEvent(uid, ev, broadcast: true);

        fixer.Blocked = ev.IsBlocked;
        fixer.Blockers.Clear();
        fixer.Blockers.AddRange(ev.Blockers);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEMurkSphereFixerComponent>();
        while (query.MoveNext(out var uid, out var fixer))
        {
            if (fixer.Blocked)
                continue;

            fixer.Charge += frameTime / (float) fixer.ChargeDuration.TotalSeconds;
            if (fixer.Charge < 1f)
                continue;

            TryFixSpheres(uid, fixer);
        }
    }

    private void TryFixSpheres(EntityUid uid, CEMurkSphereFixerComponent fixer)
    {
        var fixedAny = false;

        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
        while (sphereQuery.MoveNext(out var sphereUid, out var sphere))
        {
            if (sphere.State != CEMurkSphereState.Cracked)
                continue;

            sphere.State = CEMurkSphereState.Fixed;
            Dirty(sphereUid, sphere);
            fixedAny = true;
        }

        if (!fixedAny)
            return;

        RaiseNetworkEvent(new CEScreenPopupShowEvent(
            Loc.GetString("ce-murk-sphere-fixed-title"),
            Loc.GetString("ce-murk-sphere-fixed-desc"),
            new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")));

        _roundEndSystem.EndRound();
    }
}

/// <summary>
/// Raised on a <c>CEMurkSphereFixerComponent</c> entity whenever its block state needs
/// recomputing (see <see cref="CEMurkSphereFixerSystem.RefreshBlockConditions"/>), not every tick.
/// Subscribers should call <see cref="Block"/> if their condition isn't met - the extensible
/// point for adding new charge requirements. The title/description/coordinates feed the monitor
/// console's blocker list, so the location should point at whatever the crew needs to go fix.
/// </summary>
public sealed class CEMurkSphereFixerBlockRefreshEvent : EntityEventArgs
{
    private readonly List<CEMurkSphereFixerBlocker> _blockers = new();

    public IReadOnlyList<CEMurkSphereFixerBlocker> Blockers => _blockers;

    public bool IsBlocked => _blockers.Count > 0;

    public void Block(string title, string description, EntityCoordinates coordinates)
    {
        _blockers.Add(new CEMurkSphereFixerBlocker(title, description, coordinates));
    }
}

public readonly record struct CEMurkSphereFixerBlocker(string Title, string Description, EntityCoordinates Coordinates);
