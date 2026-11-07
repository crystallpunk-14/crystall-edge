using Content.Shared._CE.Animation.Core;
using Content.Shared._CE.Animation.Core.Components;
using Content.Shared._CE.MeleeWeapon;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._CE.StatusEffect.Parry;

public sealed partial class CEParryStatusEffectSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StatusEffectsSystem _statusEffect = default!;
    [Dependency] private CESharedAnimationActionSystem _animation = default!;

    [SubscribeLocalEvent]
    private void OnAttackAttempt(Entity<CEParryStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CEAttackAttemptEvent> args)
    {
        var attempt = args.Args;

        // Unarmed attacks have nothing to knock out and go through.
        if (attempt.Cancelled || ent.Comp.Spent || attempt.Weapon == attempt.Attacker)
            return;

        attempt.Cancel();

        // The cancel is predicted from the networked status; the consequences belong to the server.
        if (_net.IsClient)
            return;

        ent.Comp.Spent = true;
        Dirty(ent);

        var defender = args.AppliedTo;
        _audio.PlayPvs(ent.Comp.Sound, defender);
        _popup.PopupEntity(Loc.GetString("ce-parry-success"), defender, PopupType.Medium);

        DisarmAttacker(ent.Comp, attempt.Attacker, attempt.Weapon);
        EndStance(ent, defender);
    }

    private void DisarmAttacker(CEParryStatusEffectComponent parry, EntityUid attacker, EntityUid weapon)
    {
        if (_hands.IsHolding(attacker, weapon) && _hands.TryDrop(attacker, weapon, checkActionBlocker: false))
        {
            var distance = _random.NextFloat(parry.MinThrowDistance, parry.MaxThrowDistance);
            _throwing.TryThrow(weapon, _random.NextAngle().ToWorldVec() * distance, parry.ThrowSpeed, recoil: false);
        }

        _stun.TryKnockdown(attacker, parry.KnockdownDuration, drop: false);
    }

    private void EndStance(EntityUid status, EntityUid defender)
    {
        // Cancelling the stance animation removes the statuses it applied, this one included.
        if (TryComp<CEActiveAnimationActionComponent>(defender, out var animation))
            _animation.CancelAnimation((defender, animation));

        if (MetaData(status).EntityPrototype is { } proto)
            _statusEffect.TryRemoveStatusEffect(defender, proto.ID);
    }
}
