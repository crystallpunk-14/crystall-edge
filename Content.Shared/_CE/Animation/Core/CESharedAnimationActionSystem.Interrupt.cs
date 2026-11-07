using Content.Shared._CE.Animation.Core.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Shared._CE.Animation.Core;

public abstract partial class CESharedAnimationActionSystem
{
    [SubscribeLocalEvent]
    private void OnInterruptDamageChanged(Entity<CEActiveAnimationActionComponent> ent, ref DamageChangedEvent args)
    {
        // Let the server state handle prediction, same as do-afters.
        if (_timing.ApplyingState)
            return;

        if (!args.InterruptsDoAfters || !args.DamageIncreased || args.DamageDelta is null)
            return;

        if (args.Origin == ent.Owner)
            return;

        if (!_proto.Resolve(ent.Comp.ActiveAnimation, out var animation) || animation.BreakOnDamage is not { } threshold)
            return;

        if (DamageSpecifier.GetPositive(args.DamageDelta).GetTotal() < threshold)
            return;

        InterruptAnimation(ent, true);
    }

    [SubscribeLocalEvent]
    private void OnInterruptStunned(Entity<CEActiveAnimationActionComponent> ent, ref StunnedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        InterruptAnimation(ent, true);
    }

    [SubscribeLocalEvent]
    private void OnInterruptMobStateChanged(Entity<CEActiveAnimationActionComponent> ent, ref MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.NewMobState == MobState.Alive)
            return;

        InterruptAnimation(ent, false);
    }

    private void InterruptAnimation(Entity<CEActiveAnimationActionComponent> ent, bool popup)
    {
        CancelAnimation(ent);

        // Damage and stuns are mostly server-side, so the popup is sent from the server only to avoid duplicates.
        if (popup && _net.IsServer)
            _popup.PopupEntity(Loc.GetString("ce-animation-interrupted"), ent, ent, PopupType.SmallCaution);
    }
}
