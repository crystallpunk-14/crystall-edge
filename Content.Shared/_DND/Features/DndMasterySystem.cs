using System.Numerics;
using Content.Shared._DND.Abilities;
using Content.Shared._DND.Combat;
using Content.Shared._DND.Equipment;
using Content.Shared._DND.Factions;
using Content.Shared._DND.Spells;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._DND.Features;

/// <summary>
/// Mastery properties for the weapon kinds a character has practiced.
/// Cleave and Nick each happen once per six seconds. Sap, Slow, and Vex last six seconds.
/// </summary>
public sealed partial class DndMasterySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DndArmorSystem _armor = default!;
    [Dependency] private DndAllegianceSystem _allegiance = default!;
    [Dependency] private DndInspirationSystem _inspiration = default!;
    [Dependency] private DndCombatSystem _combat = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<DndSlowComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var slow))
        {
            if (now < slow.Until)
                continue;

            RemComp<DndSlowComponent>(uid);
            _speed.RefreshMovementSpeedModifiers(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnMove(EntityUid uid, DndSlowComponent component, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_timing.CurTime >= component.Until)
            return;

        args.ModifySpeed(DndRageRules.SlowedSpeed);
    }

    [SubscribeLocalEvent]
    private void OnAttack(EntityUid uid, DndWeaponMasteryComponent component, ref DndAttackResolvedEvent args)
    {
        if (!_net.IsServer || args.Depth > 2)
            return;

        if (args.Outcome == DndAttackOutcome.Miss && Knows(component, args.Profile, DndWeaponMastery.Graze))
        {
            var graze = DndRageRules.Graze(DndAbilityScores.Modifier(args.AbilityScore));
            _combat.ReductionOf(args.Target, args.Profile.DamageType, out var resistant, out var vulnerable, out var immune);
            var amount = DndCombat.ApplyDamage(graze, 0, false, 0, resistant, vulnerable, immune);
            args.Damage = _combat.TypeDamage(args.Profile.DamageType, amount);
            if (amount > 0)
                _popup.PopupEntity(Loc.GetString("dnd-mastery-graze", ("damage", amount)), args.Target, PopupType.Small);
        }

        if (args.Outcome != DndAttackOutcome.Miss && args.Damage.AnyPositive())
        {
            if (Knows(component, args.Profile, DndWeaponMastery.Push))
                Push(uid, args.Target);

            if (Knows(component, args.Profile, DndWeaponMastery.Sap))
                Sap(args.Target);

            if (Knows(component, args.Profile, DndWeaponMastery.Slow))
                Slow(args.Target);

            if (Knows(component, args.Profile, DndWeaponMastery.Topple))
                Topple(uid, args.Target, args.AbilityScore);

            if (Knows(component, args.Profile, DndWeaponMastery.Vex))
            {
                component.VexTarget = args.Target;
                component.VexUntil = _timing.CurTime + TimeSpan.FromSeconds(DndRageRules.RoundSeconds);
            }
        }

        if (args.AllowCleave && args.Outcome != DndAttackOutcome.Miss && Knows(component, args.Profile, DndWeaponMastery.Cleave) && _timing.CurTime >= component.CleaveReadyAt)
        {
            component.CleaveReadyAt = _timing.CurTime + TimeSpan.FromSeconds(DndRageRules.RoundSeconds);
            Cleave(uid, args);
        }

        if (args.AllowNick && Knows(component, args.Profile, DndWeaponMastery.Nick) && _timing.CurTime >= component.NickReadyAt)
        {
            if (FindLightWeapon(uid, args.Weapon) is { } offhand)
            {
                component.NickReadyAt = _timing.CurTime + TimeSpan.FromSeconds(DndRageRules.RoundSeconds);
                var dealt = _combat.RollAttack(uid, args.Target, offhand, allowCleave: true, allowNick: false, depth: args.Depth + 1);
                if (!dealt.Empty)
                    _damageable.TryChangeDamage(args.Target, dealt, origin: uid);
            }
        }
    }

    private void Cleave(EntityUid attacker, DndAttackResolvedEvent args)
    {
        if (FindCleaveTarget(attacker, args.Target, args.Profile.RangeFeet) is not { } second)
            return;

        var modifier = DndAbilityScores.Modifier(args.AbilityScore);
        var profile = args.Profile;
        if (!DndRageRules.CleaveAddsModifier(modifier))
            profile = profile with { AddAbilityModifier = false };

        var dealt = _combat.RollAttack(attacker, second, args.Weapon, allowCleave: false, allowNick: false, depth: args.Depth + 1, profileOverride: profile);
        if (!dealt.Empty)
            _damageable.TryChangeDamage(second, dealt, origin: attacker);
    }

    private EntityUid? FindCleaveTarget(EntityUid attacker, EntityUid first, int reachFeet)
    {
        foreach (var candidate in _lookup.GetEntitiesInRange(Transform(first).Coordinates, DndCombat.Tiles(5), LookupFlags.Uncontained))
        {
            if (candidate == attacker || candidate == first)
                continue;

            if (!HasComp<MobStateComponent>(candidate) || !_allegiance.IsOpposition(attacker, candidate))
                continue;

            if (!Within(attacker, candidate, reachFeet) || !Within(first, candidate, 5))
                continue;

            return candidate;
        }

        return null;
    }

    private bool Within(EntityUid from, EntityUid to, int feet)
    {
        var delta = _transform.GetWorldPosition(from) - _transform.GetWorldPosition(to);
        return delta.Length() <= DndCombat.Tiles(feet) + 1f;
    }

    private EntityUid? FindLightWeapon(EntityUid attacker, EntityUid used)
    {
        if (!TryComp<HandsComponent>(attacker, out var hands))
            return null;

        foreach (var held in _hands.EnumerateHeld((attacker, hands)))
        {
            if (held == used)
                continue;

            if (TryComp<DndWeaponComponent>(held, out var weapon) && weapon.Light && !weapon.Ranged)
                return held;
        }

        return null;
    }

    private void Push(EntityUid attacker, EntityUid target)
    {
        var size = DndCreatureSize.Medium;
        if (TryComp<DndCreatureSizeComponent>(target, out var sized))
            size = sized.Size;

        if (!DndRageRules.CanPush(size))
            return;

        var delta = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(attacker);
        if (delta.LengthSquared() < 0.01f)
            delta = new Vector2(1f, 0f);

        var push = Vector2.Normalize(delta) * DndCombat.Tiles(10);
        _throwing.TryThrow(target, push, user: attacker, pushbackRatio: 0f, compensateFriction: true, recoil: false, doSpin: false, playSound: false);
    }

    private void Sap(EntityUid target)
    {
        var sap = EnsureComp<DndSapComponent>(target);
        var until = _timing.CurTime + TimeSpan.FromSeconds(DndRageRules.RoundSeconds);
        if (until > sap.Until)
            sap.Until = until;

        _popup.PopupEntity(Loc.GetString("dnd-mastery-sap"), target, PopupType.Small);
    }

    private void Slow(EntityUid target)
    {
        var slow = EnsureComp<DndSlowComponent>(target);
        var until = _timing.CurTime + TimeSpan.FromSeconds(DndRageRules.RoundSeconds);
        if (until > slow.Until)
            slow.Until = until;

        Dirty(target, slow);
        _speed.RefreshMovementSpeedModifiers(target);
        _popup.PopupEntity(Loc.GetString("dnd-mastery-slow"), target, PopupType.Small);
    }

    private void Topple(EntityUid attacker, EntityUid target, int abilityScore)
    {
        if (!TryComp<DndAbilitiesComponent>(attacker, out var abilities))
            return;

        var dc = DndChecks.DifficultyClass(abilityScore, abilities.ProficiencyBonus);
        if (SavingThrow(target, DndAbilityPrototype.ConstitutionId, dc))
        {
            _popup.PopupEntity(Loc.GetString("dnd-mastery-topple-save", ("dc", dc)), target, PopupType.Small);
            return;
        }

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(DndRageRules.RoundSeconds), refresh: true, autoStand: true, drop: false);
        _popup.PopupEntity(Loc.GetString("dnd-mastery-topple", ("dc", dc)), target, PopupType.Small);
    }

    private bool SavingThrow(EntityUid defender, string abilityId, int difficultyClass)
    {
        var score = 10;
        var proficiency = 0;
        if (TryComp<DndAbilitiesComponent>(defender, out var abilities))
        {
            score = abilities.GetScore(abilityId);
            proficiency = abilities.ProficiencyBonus;
        }

        var proficient = false;
        if (TryComp<DndTrainingComponent>(defender, out var training))
        {
            foreach (var save in training.SavingThrows)
            {
                if (save.Id == abilityId)
                {
                    proficient = true;
                    break;
                }
            }
        }

        var advantage = abilityId == DndAbilityPrototype.StrengthId && TryComp<DndRageComponent>(defender, out var rage) && rage.Active;
        var disadvantage = (abilityId == DndAbilityPrototype.StrengthId || abilityId == DndAbilityPrototype.DexterityId)
            && _armor.HasPenalty(defender);
        if (TryComp<DndBaneComponent>(defender, out var bane) && _timing.CurTime < bane.Until)
            disadvantage = true;

        var first = _random.Next(1, DndCombat.DieSides + 1);
        var second = first;
        if (advantage != disadvantage)
            second = _random.Next(1, DndCombat.DieSides + 1);

        var roll = DndChecks.KeptRoll(first, second, advantage, disadvantage);
        var bonus = DndChecks.Bonus(score, proficient, proficiency) + DndSpellBuffs.RollDie(defender, EntityManager, _timing, _random);
        var total = _inspiration.Improve(defender, roll, bonus, difficultyClass);
        return total >= difficultyClass;
    }

    private bool Knows(DndWeaponMasteryComponent mastery, DndWeaponProfile profile, DndWeaponMastery property)
    {
        if (profile.Mastery != property || profile.Kind == null)
            return false;

        foreach (var kind in mastery.Kinds)
        {
            if (kind.Id == profile.Kind)
                return true;
        }

        return false;
    }

    public bool SwapOne(EntityUid uid, DndWeaponMasteryComponent mastery)
    {
        if (mastery.Kinds.Count == 0 || mastery.SwapsRemaining <= 0)
        {
            _popup.PopupEntity(Loc.GetString("dnd-mastery-drill-none"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        var allowed = new List<string>();
        foreach (var kind in _prototypes.EnumeratePrototypes<DndWeaponKindPrototype>())
        {
            if (kind.Melee && (kind.Training == DndWeaponTraining.Simple || kind.Training == DndWeaponTraining.Martial))
                allowed.Add(kind.ID);
        }

        allowed.Sort(StringComparer.Ordinal);
        if (allowed.Count == 0)
            return false;

        var index = mastery.Kinds.Count - 1;
        var current = mastery.Kinds[index].Id;
        var start = 0;
        for (var i = 0; i < allowed.Count; i++)
        {
            if (allowed[i] == current)
            {
                start = i;
                break;
            }
        }

        string? next = null;
        for (var step = 1; step <= allowed.Count; step++)
        {
            var candidate = allowed[(start + step) % allowed.Count];
            if (candidate == current)
                continue;

            var taken = false;
            foreach (var kind in mastery.Kinds)
            {
                if (kind.Id == candidate)
                {
                    taken = true;
                    break;
                }
            }

            if (taken)
                continue;

            next = candidate;
            break;
        }

        if (next == null)
        {
            _popup.PopupEntity(Loc.GetString("dnd-mastery-drill-none"), uid, uid, PopupType.Small);
            return false;
        }

        mastery.Kinds[index] = next;
        mastery.SwapsRemaining--;
        if (_prototypes.TryIndex<DndWeaponKindPrototype>(next, out var prototype))
            _popup.PopupEntity(Loc.GetString("dnd-mastery-drill", ("weapon", Loc.GetString(prototype.Name))), uid, uid, PopupType.Medium);

        return true;
    }
}
