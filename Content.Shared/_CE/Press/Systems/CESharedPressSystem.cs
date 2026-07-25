using Content.Shared._CE.Press.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.Power;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Press.Systems;

public abstract partial class CESharedPressSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private DamageableSystem _damageable = default!;
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
        var tileRef = _turf.GetTileRef(Transform(uid).Coordinates);
        var scanned = tileRef is null
            ? new HashSet<EntityUid>()
            : new HashSet<EntityUid>(_lookup.GetLocalEntitiesIntersecting(tileRef.Value, flags: LookupFlags.All));

        scanned.Remove(uid);

        EntityUid? target = null;
        foreach (var scannedUid in scanned)
        {
            if (HasComp<CEPressTargetComponent>(scannedUid) && Transform(scannedUid).Anchored)
            {
                target = scannedUid;
                break;
            }
        }

        if (target is { } targetUid)
        {
            var others = new HashSet<EntityUid>(scanned);
            others.Remove(targetUid);

            var ev = new CEPressCrushingTargetEvent(uid, others);
            RaiseLocalEvent(targetUid, ev);
        }
        else
        {
            foreach (var scannedUid in scanned)
            {
                _damageable.TryChangeDamage(scannedUid, press.CrushDamage);
            }
        }

        if (_net.IsClient && press.CrushVFX is { } vfx)
            SpawnAtPosition(vfx, Transform(uid).Coordinates);

        press.State = CEPressState.Recovering;
        press.StateEndTime = _timing.CurTime + press.RecoveringDuration;
        Dirty(uid, press);
    }
}

/// <summary>
/// Raised on a CEPressTargetComponent entity found on a press's tile when the press finishes
/// crushing. Carries the press itself and every other entity found on the same tile (excluding
/// the press and this target) so the target platform can decide what to do with them.
/// </summary>
public sealed partial class CEPressCrushingTargetEvent(EntityUid press, HashSet<EntityUid> entities) : EntityEventArgs
{
    public EntityUid Press = press;
    public HashSet<EntityUid> Entities = entities;
}
