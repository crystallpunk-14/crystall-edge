using Content.Shared._CE.Press.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.Power;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Press.Systems;

public abstract partial class CESharedPressSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEPressComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<CEPressComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            if (ent.Comp.State != CEPressState.Idle)
                return;

            ent.Comp.State = CEPressState.Preparing;
            ent.Comp.StateEndTime = _timing.CurTime + ent.Comp.PreparingDuration;
            Dirty(ent);
        }
        else
        {
            if (ent.Comp.State == CEPressState.Idle)
                return;

            ent.Comp.State = CEPressState.Idle;
            ent.Comp.StateEndTime = TimeSpan.Zero;
            Dirty(ent);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEPressComponent>();
        while (query.MoveNext(out var uid, out var press))
        {
            if (!_timing.IsFirstTimePredicted)
                continue;

            switch (press.State)
            {
                case CEPressState.Idle:
                case CEPressState.Crushing:
                    continue;

                case CEPressState.Preparing:
                    if (_timing.CurTime < press.StateEndTime)
                        continue;

                    press.State = CEPressState.Crushing;
                    Dirty(uid, press);
                    Crush(uid, press);
                    break;

                case CEPressState.Recovering:
                    if (_timing.CurTime < press.StateEndTime)
                        continue;

                    press.State = CEPressState.Preparing;
                    press.StateEndTime = _timing.CurTime + press.PreparingDuration;
                    Dirty(uid, press);
                    break;
            }
        }
    }

    private void Crush(EntityUid uid, CEPressComponent press)
    {
        if (_net.IsClient)
            return; //TODO: Proper prediction

        var tileRef = _turf.GetTileRef(Transform(uid).Coordinates);
        var scanned = tileRef is null
            ? new HashSet<EntityUid>()
            : new HashSet<EntityUid>(_lookup.GetLocalEntitiesIntersecting(tileRef.Value, flags: LookupFlags.All));

        scanned.Remove(uid);

        // Contained entities are otherwise fair game (e.g. money inside a wallet lying on the
        // tile), but the press's own machine board/parts sit in its own containers and would
        // spatially resolve to this same tile - exclude those specifically.
        scanned.RemoveWhere(e => _container.ContainsEntity(uid, e));

        EntityUid? target = null;
        foreach (var scannedUid in scanned)
        {
            if (HasComp<CEPressTargetComponent>(scannedUid) && Transform(scannedUid).Anchored)
            {
                target = scannedUid;
                break;
            }
        }

        // Same reasoning as above, but for whatever target platform we found - its own machine
        // board/parts shouldn't be crushed either.
        if (target is { } foundTarget)
            scanned.RemoveWhere(e => _container.ContainsEntity(foundTarget, e));

        var crushed = new HashSet<EntityUid>();
        foreach (var scannedUid in scanned)
        {
            if (scannedUid == target || Transform(scannedUid).Anchored)
                continue;

            crushed.Add(scannedUid);
        }

        if (target is { } targetUid)
        {
            var ev = new CEPressCrushingTargetEvent(uid, crushed);
            RaiseLocalEvent(targetUid, ev);
            FallbackCrush(ev.Entities, press);
        }
        else
        {
            FallbackCrush(crushed, press);
        }

        if (press.CrushVFX is { } vfx)
            SpawnAtPosition(vfx, Transform(uid).Coordinates);

        press.State = CEPressState.Recovering;
        press.StateEndTime = _timing.CurTime + press.RecoveringDuration;
        Dirty(uid, press);
    }

    /// <summary>
    /// Applies CrushDamage and scatters with a random throw. Used both when there's no target at
    /// all, and for whatever a target platform's CEPressCrushingTargetEvent subscribers left
    /// unhandled. The throw itself is server-only: IRobustRandom isn't synced between client and
    /// server, so a client-predicted throw direction would just get overwritten/mispredicted once
    /// the server's authoritative throw arrives anyway.
    /// </summary>
    private void FallbackCrush(IEnumerable<EntityUid> entities, CEPressComponent press)
    {
        foreach (var crushedUid in entities)
        {
            _damageable.TryChangeDamage(crushedUid, press.CrushDamage);

            if (_net.IsServer)
                _throwing.TryThrow(crushedUid, _random.NextVector2(), press.CrushThrowSpeed, doSpin: true);
        }
    }
}

/// <summary>
/// Raised on a CEPressTargetComponent entity found on a press's tile when the press finishes
/// crushing. Carries the press itself and every other non-anchored entity found on the same tile
/// (excluding the press and this target). Subscribers should Remove() any entity they've handled
/// from Entities; whatever is left when the event returns is considered unhandled and gets
/// CrushDamage and a scatter throw applied by the press as a fallback.
/// </summary>
public sealed partial class CEPressCrushingTargetEvent(EntityUid press, HashSet<EntityUid> entities) : EntityEventArgs
{
    public EntityUid Press = press;
    public HashSet<EntityUid> Entities = entities;
}
