using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.Animation.Core;
using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.EntityEffect;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CE.MeleeWeapon;

public abstract partial class CESharedWeaponSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected IMapManager MapManager = default!;
    [Dependency] protected ActionBlockerSystem Blocker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] protected SharedCombatModeSystem CombatMode = default!;
    [Dependency] protected SharedInteractionSystem Interaction = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] private CESharedAnimationActionSystem _animationAction = default!;
    [Dependency] protected IPrototypeManager _proto = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<CEWeaponUseEvent>(OnClientAttackRequest);
        SubscribeAllEvent<CEStopWeaponUseEvent>(OnClientStopRequest);
        SubscribeAllEvent<CEWeaponArcHitEvent>(OnArcHitEvent);
    }

    private void OnClientAttackRequest(CEWeaponUseEvent ev, EntitySessionEventArgs args)
    {
        if (Timing.ApplyingState)
            return;

        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (!TryGetWeapon(user, out var weapon) ||
            weapon.Value.Owner != GetEntity(ev.Weapon))
            return;

        TryUse(user, weapon.Value, ev.UseType, ev.Angle);
    }

    private void OnClientStopRequest(CEStopWeaponUseEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        if (!TryGetWeapon(user.Value, out var weapon) ||
            weapon.Value.Owner != GetEntity(ev.Weapon))
            return;

        if (!weapon.Value.Comp.Using)
            return;

        weapon.Value.Comp.Using = false;
        DirtyField(weapon.Value.Owner, weapon.Value.Comp, nameof(CEWeaponComponent.Using));
    }

    private void OnArcHitEvent(CEWeaponArcHitEvent ev, EntitySessionEventArgs args)
    {
        if (Timing.ApplyingState)
            return;

        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var weaponEntity = GetEntity(ev.Weapon);
        if (!TryComp<CEWeaponComponent>(weaponEntity, out var weaponComp))
            return;

        // Validate the user holds this weapon in any hand (supports off-hand dual-wield attacks)
        var userHoldsWeapon = false;
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (held == weaponEntity) { userHoldsWeapon = true; break; }
        }
        if (!userHoldsWeapon) return;

        var weapon = new Entity<CEWeaponComponent>(weaponEntity, weaponComp);
        var targets = GetEntityList(ev.Targets);
        targets = ValidateArcTargets(user, weapon, targets, args.SenderSession);

        TryAttack(user, weapon, targets);
        ApplyArcEffects(user, weapon, targets, ev.EffectSlot, ev.Power);
    }

    /// <summary>
    /// Validates arc attack targets. Server overrides to check range and obstructions.
    /// </summary>
    protected virtual List<EntityUid> ValidateArcTargets(EntityUid user, Entity<CEWeaponComponent> weapon, List<EntityUid> targets, ICommonSession? session)
    {
        return targets;
    }

    /// <summary>
    /// Runs effects from the weapon's EffectSlot on validated targets.
    /// Server overrides to apply damage from the weapon's EffectSlot data.
    /// Client base does nothing — effects are applied in the Effect() loop during prediction.
    /// </summary>
    protected void ApplyArcEffects(EntityUid user, Entity<CEWeaponComponent> weapon, List<EntityUid> targets, string? effectSlot, float power = 1f)
    {
        if (effectSlot == null
            || !weapon.Comp.EffectSlots.TryGetValue(effectSlot, out var slotEffects)
            || targets.Count == 0)
            return;

        foreach (var target in targets)
        {
            var effectArgs = new CEEntityEffectArgs(
                EntityManager,
                user,
                weapon.Owner,
                Angle.Zero,
                1f,
                target,
                null,
                power);

            foreach (var slotEffect in slotEffects)
            {
                slotEffect.Effect(effectArgs);
            }
        }
    }

    public bool TryUse(
        EntityUid user,
        Entity<CEWeaponComponent> used,
        CEUseType useType,
        Angle angle)
    {
        var curTime = Timing.CurTime;

        if (!Blocker.CanAttack(user))
            return false;

        if (_animationAction.IsPlayingAnimation(user))
            return false;

        //Get animations
        List<CEAnimationEntry> animations = new();

        var animEv = new CEGetWeaponAnimationsEvent(used, useType, user);
        RaiseLocalEvent(used, animEv);

        if (animEv.Handled && animEv.Animations.Count != 0)
            animations = animEv.Animations;
        else //Get default animations
        {
            if (used.Comp.Animations.TryGetValue(useType, out var a))
                animations = a;
        }

        if (animations.Count == 0)
            return false;

        // Determine combo index.
        // Reset if: different use type, or combo deadline expired.
        var comboIndex = 0;
        if (used.Comp.LastComboUseType == useType && curTime < used.Comp.ComboResetDeadline)
            comboIndex = used.Comp.ComboIndex % animations.Count;

        var entry = animations[comboIndex];

        // Check all cost components (stamina, mana, charges, etc.)
        var attemptEv = new CEWeaponUseAttemptEvent(user, useType);
        RaiseLocalEvent(used, attemptEv);
        if (attemptEv.Cancelled)
            return false;

        var animationProtoId = entry.Anim;

        var animationSpeed = GetAnimationSpeed(user, used) * entry.Speed;
        if (!_animationAction.TryPlayAnimationToAngle(user, animationProtoId, angle, used.Owner, animationSpeed))
            return false;

        // Consume resources after animation starts
        var usedEv = new CEWeaponUsedEvent(user, useType);
        RaiseLocalEvent(used, usedEv);

        // Calculate the deadline: animation duration * 1.5 (adjusted for playback speed).
        var animDuration = _proto.Index(animationProtoId).Duration;
        used.Comp.LastComboUseType = useType;
        used.Comp.ComboIndex = comboIndex + 1;
        used.Comp.ComboResetDeadline = curTime + (animDuration / animationSpeed) * 1.5;
        used.Comp.Using = true;
        Dirty(used);

        return true;
    }

    public bool TryGetWeapon(EntityUid entity, [NotNullWhen(true)] out Entity<CEWeaponComponent>? used)
    {
        used = null;

        var ev = new CEGetWeaponEvent();
        RaiseLocalEvent(entity, ev);
        if (ev.Handled && ev.Used != null)
        {
            used = ev.Used;
            return true;
        }

        // Use in-hands entity if available.
        if (_hands.TryGetActiveItem(entity, out var held) &&
            TryComp<CEWeaponComponent>(held, out var heldWeapon))
        {
            used = (held.Value, heldWeapon);
            return true;
        }

        // Use own body.
        if (TryComp<CEWeaponComponent>(entity, out var melee))
        {
            used = (entity, melee);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the animation playback speed, where 1 = 100% speed, 2 = 200% speed
    /// </summary>
    private float GetAnimationSpeed(EntityUid entity, Entity<CEWeaponComponent> used)
    {
        var ev = new CEGetWeaponSpeedEvent();
        RaiseLocalEvent(entity, ev);
        RaiseLocalEvent(used, ev);

        var speed = ev.GetSpeed();
        return speed;
    }

    /// <summary>
    /// Called from <see cref="Content.Shared._CE.EntityEffect.Effects.WeaponArcAttack"/> when arc trace detects targets.
    /// Client overrides to send hit list to server. Server overrides to skip (waits for client event)
    /// unless the attacker is an NPC.
    /// </summary>
    public virtual void HandleArcAttackHit(EntityUid user, Entity<CEWeaponComponent> weapon, List<EntityUid> targets, string? effectSlot, float power = 1f)
    {
        TryAttack(user, weapon, targets);
    }

    public bool TryAttack(EntityUid user, Entity<CEWeaponComponent> weapon, List<EntityUid> targets)
    {
        // Only consider entities that can be attacked (have a damageable component).
        var valid = new List<EntityUid>();
        foreach (var target in targets)
        {
            if (!HasComp<DamageableComponent>(target))
                continue;

            valid.Add(target);
        }

        if (valid.Count == 0)
            return false;

        foreach (var target in valid)
        {
            var attackedEv = new CEAttackedEvent(user, weapon);
            RaiseLocalEvent(target, attackedEv);
        }

        _audio.PlayPredicted(weapon.Comp.HitSound, weapon, user);

        var usedEv = new CEAttackUsingEvent(user, valid);
        RaiseLocalEvent(weapon, usedEv);

        var attackerEv = new CEAfterAttackEvent(weapon, valid);
        RaiseLocalEvent(user, attackerEv);

        return true;
    }
}

/// <summary>
/// Raised on used weapon when attack hits something.
/// </summary>
public sealed partial class CEAttackUsingEvent(EntityUid user, List<EntityUid> targets) : EntityEventArgs
{
    public EntityUid User = user;
    public List<EntityUid> Targets = targets;
}

/// <summary>
/// Raised on attacked entity when it gets hit by a CEWeaponComponent attack.
/// </summary>
public sealed partial class CEAttackedEvent(EntityUid attacker, EntityUid weapon) : EntityEventArgs
{
    public EntityUid Attacker = attacker;
    public EntityUid Weapon = weapon;
}

/// <summary>
/// Raised on attacker, after it attacks something with a CEWeaponComponent
/// </summary>
public sealed partial class CEAfterAttackEvent(EntityUid weapon, List<EntityUid> targets) : EntityEventArgs
{
    public EntityUid Weapon = weapon;
    public List<EntityUid> Targets = targets;
}
