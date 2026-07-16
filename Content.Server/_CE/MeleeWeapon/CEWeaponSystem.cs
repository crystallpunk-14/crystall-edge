using Content.Server.Movement.Systems;
using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared._CE.MeleeWeapon;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._CE.MeleeWeapon;

public sealed partial class CEWeaponSystem : CESharedWeaponSystem
{
    [Dependency] private LagCompensationSystem _lag = default!;
    private const int MaxTargets = 10;

    /// <summary>
    /// Extra tolerance added to the weapon's effective range for server validation.
    /// Accounts for network latency and position prediction differences.
    /// </summary>
    private const float RangeTolerance = 0.2f;

    /// <summary>
    /// Fallback validation range when no WeaponArcAttack effect is found on the weapon.
    /// </summary>
    private const float FallbackRange = 1f;

    /// <summary>
    /// For player attacks, skip damage from the animation keyframe.
    /// Damage comes from the predicted <see cref="CEWeaponArcHitEvent"/> handled in the shared system.
    /// For NPCs (no attached session), apply damage directly since there is no client.
    /// </summary>
    public override void HandleArcAttackHit(EntityUid user, Entity<CEWeaponComponent> weapon, List<EntityUid> targets, string? effectSlot, float power = 1f)
    {
        if (HasComp<ActorComponent>(user))
        {
            // Clear targets so the nested effects loop in Effect() does nothing.
            // Damage will be applied via OnArcHitEvent -> ApplyArcEffects instead.
            targets.Clear();
            return;
        }

        TryAttack(user, weapon, targets);
        ApplyArcEffects(user, weapon, targets, effectSlot, power);
    }

    protected override List<EntityUid> ValidateArcTargets(EntityUid user, Entity<CEWeaponComponent> weapon, List<EntityUid> targets, ICommonSession? session)
    {
        if (targets.Count > MaxTargets)
            targets = targets.GetRange(0, MaxTargets);

        var range = GetMaxEffectiveRange(weapon) + RangeTolerance;
        var validated = new List<EntityUid>();

        foreach (var target in targets)
        {
            if (!Exists(target) || target == user)
                continue;

            if (session is { } pSession)
            {
                EntityCoordinates targetCoordinates;
                Angle targetLocalAngle;

                (targetCoordinates, targetLocalAngle) = _lag.GetCoordinatesAngle(target, pSession);
                if (!Interaction.InRangeUnobstructed(user, target, targetCoordinates, targetLocalAngle, range, overlapCheck: false))
                    continue;
            }
            else if (!Interaction.InRangeUnobstructed(user, target, range))
                continue;

            validated.Add(target);
        }

        return validated;
    }

    /// <summary>
    /// Computes the maximum effective range (base range * max multiplier * 2) from all WeaponArcAttack
    /// effects defined in the weapon's animation keyframes.
    /// </summary>
    private float GetMaxEffectiveRange(Entity<CEWeaponComponent> weapon)
    {
        var maxMultiplier = 0f;

        foreach (var entries in weapon.Comp.Animations.Values)
        {
            foreach (var entry in entries)
            {
                if (!_proto.TryIndex(entry.Anim, out var anim))
                    continue;

                foreach (var effects in anim.Events.Values)
                {
                    foreach (var effect in effects)
                    {
                        if (effect is WeaponArcAttack arc && arc.RangeMultiplier > maxMultiplier)
                            maxMultiplier = arc.RangeMultiplier;
                    }
                }
            }
        }

        if (maxMultiplier <= 0f)
            return weapon.Comp.Range > 0f ? weapon.Comp.Range * 2 : FallbackRange;

        return weapon.Comp.Range * maxMultiplier * 2;
    }
}
