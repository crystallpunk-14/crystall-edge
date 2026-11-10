using Content.Server.Power.EntitySystems;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.StrapEffect;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Power;
using Robust.Server.Audio;
using Robust.Shared.Timing;

namespace Content.Server._CE.StrapEffect;

public sealed partial class CEStrapEffectSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEStrapEffectComponent, StrapComponent>();
        while (query.MoveNext(out var uid, out var effect, out var strap))
        {
            if (effect.ActiveDoAfter is not null || _timing.CurTime < effect.NextCycleTime)
                continue;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            EntityUid? target = null;
            foreach (var buckled in strap.BuckledEntities)
            {
                target = buckled;
                break;
            }

            if (target is null)
                continue;

            var doAfterArgs = new DoAfterArgs(EntityManager,
                uid,
                effect.Duration,
                new CEStrapEffectDoAfterEvent(),
                uid,
                target,
                uid)
            {
                BreakOnMove = false,
                BreakOnDamage = false,
                NeedHand = false,
                RequireCanInteract = false,
            };

            if (!_doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId))
                continue;

            effect.ActiveDoAfter = doAfterId;
            effect.NextCycleTime = _timing.CurTime + effect.Duration + effect.Cooldown;
            _audio.PlayPvs(effect.StartSound, uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<CEStrapEffectComponent> ent, ref CEStrapEffectDoAfterEvent args)
    {
        ent.Comp.ActiveDoAfter = null;

        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp<BuckleComponent>(target, out var buckle) || buckle.BuckledTo != ent.Owner)
            return;

        args.Handled = true;

        var coords = Transform(target).Coordinates;
        var effectArgs = new CEEntityEffectArgs(EntityManager, ent, ent, Angle.Zero, 0f, target, coords);
        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(effectArgs);
        }

        _audio.PlayPvs(ent.Comp.FinishSound, ent);

        if (ent.Comp.Vfx is { } vfx)
            SpawnAtPosition(vfx, coords);
    }

    [SubscribeLocalEvent]
    private void OnUnstrapped(Entity<CEStrapEffectComponent> ent, ref UnstrappedEvent args)
    {
        BreakCycle(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CEStrapEffectComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            BreakCycle(ent);
    }

    private void BreakCycle(Entity<CEStrapEffectComponent> ent)
    {
        if (ent.Comp.ActiveDoAfter is not { } doAfter)
            return;

        _doAfter.Cancel(doAfter);
        ent.Comp.ActiveDoAfter = null;
        ent.Comp.NextCycleTime = _timing.CurTime + ent.Comp.Cooldown;
    }
}
