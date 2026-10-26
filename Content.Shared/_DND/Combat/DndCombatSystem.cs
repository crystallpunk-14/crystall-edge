using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared._DND.Abilities;
using Content.Shared._DND.Backgrounds;
using Content.Shared._DND.Classes;
using Content.Shared._DND.Equipment;
using Content.Shared._DND.Factions;
using Content.Shared._DND.Features;
using Content.Shared._DND.Spells;
using Content.Shared.Preferences;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._DND.Combat;

/// <summary>
/// Rolls attacks for characters with ability scores. A creature does not harm its own side.
/// </summary>
public sealed partial class DndCombatSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DndArmorSystem _armor = default!;
    [Dependency] private DndAllegianceSystem _allegiance = default!;
    [Dependency] private DndInspirationSystem _inspiration = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    [SubscribeLocalEvent]
    private void OnWeaponMapInit(Entity<CEWeaponComponent> ent, ref MapInitEvent args)
    {
        EnsureWeapon(ent.Owner, rangedFallback: false);
    }

    [SubscribeLocalEvent]
    private void OnGunMapInit(Entity<GunComponent> ent, ref MapInitEvent args)
    {
        EnsureWeapon(ent.Owner, rangedFallback: !HasComp<CEWeaponComponent>(ent));
    }

    [SubscribeLocalEvent]
    private void OnBeforeDamage(Entity<DamageableComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Origin is not { } origin || origin == ent.Owner || !args.Damage.AnyPositive())
            return;

        if (!_allegiance.CanHarm(origin, ent.Owner))
        {
            args.Cancelled = true;
            return;
        }

        var spells = EntityManager.System<DndSpellSystem>();
        spells.AbsorbTemporaryHitPoints(ent.Owner, args.Damage);
    }

    [SubscribeLocalEvent]
    private void OnOutgoingDamage(EntityUid uid, DndAbilitiesComponent component, CEGetOutgoingDamageEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        if (!_net.IsServer)
        {
            args.Damage = new DamageSpecifier();
            return;
        }

        var weapon = args.Used ?? uid;
        args.Damage = RollAttack(uid, args.Target, weapon);
    }

    [SubscribeLocalEvent]
    private void OnProjectileHit(Entity<ProjectileComponent> ent, ref BeforeProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter || !HasComp<DndAbilitiesComponent>(shooter))
            return;

        if (!args.Damage.AnyPositive())
            return;

        var weapon = ent.Comp.Weapon ?? ent.Owner;
        args.Damage = RollAttack(shooter, args.Target, weapon);
    }

    [SubscribeLocalEvent]
    private void OnHitscanAttempt(Entity<HitscanBasicDamageComponent> ent, ref AttemptHitscanRaycastFiredEvent args)
    {
        if (args.Data.Shooter is not { } shooter || args.Data.HitEntity is not { } target)
            return;

        if (!HasComp<DndAbilitiesComponent>(shooter))
        {
            if (!_allegiance.CanHarm(shooter, target))
                args.Cancelled = true;
            return;
        }

        args.Cancelled = true;
        if (!_net.IsServer)
            return;

        var dealt = RollAttack(shooter, target, args.Data.Gun, pipeline: false);
        if (dealt.Empty)
            return;

        _damageable.TryChangeDamage(target, dealt, origin: shooter, ignoreResistances: true);
    }

    [SubscribeLocalEvent]
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!_prototypes.TryIndex(ev.Profile.DndClass, out DndClassPrototype? prototype) || prototype.SpawnAsGhost)
            return;

        var max = DndCombat.HitPoints(prototype.HitDie, Constitution(ev.Profile), DndAbilityScores.StartingLevel);
        var hitPoints = EnsureComp<DndHitPointsComponent>(ev.Mob);
        hitPoints.Max = max;
        Dirty(ev.Mob, hitPoints);

        if (!HasComp<MobThresholdsComponent>(ev.Mob))
            return;

        var threshold = FixedPoint2.New(max);
        _thresholds.SetMobStateThreshold(ev.Mob, threshold, MobState.Critical);
        _thresholds.SetMobStateThreshold(ev.Mob, threshold, MobState.Dead);
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<DndHitPointsComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<DndAbilitiesComponent>(ent, out var abilities))
            return;

        args.PushText(Loc.GetString(
            "dnd-examine-combat",
            ("hp", ent.Comp.Max),
            ("ac", _armor.ArmorClass(ent.Owner, abilities)),
            ("proficiency", DndAbilityScores.FormatSigned(abilities.ProficiencyBonus))));
    }

    private void EnsureWeapon(EntityUid uid, bool rangedFallback)
    {
        if (HasComp<DndWeaponComponent>(uid))
        {
            ApplyRange(uid);
            return;
        }

        if (MetaData(uid).EntityPrototype is not { } proto)
            return;

        var weapon = AddComp<DndWeaponComponent>(uid);
        DndWeaponCatalog.Fill(_prototypes, proto, weapon, HasComp<MobStateComponent>(uid), rangedFallback);
        ApplyRange(uid);
    }

    private void ApplyRange(EntityUid uid)
    {
        if (!TryComp<DndWeaponComponent>(uid, out var weapon) || weapon.Ranged)
            return;

        if (!TryComp<CEWeaponComponent>(uid, out var melee))
            return;

        melee.Range = DndCombat.MeleeStoredRange(weapon.RangeFeet);
        Dirty(uid, melee);
    }

    public DamageSpecifier RollAttack(
        EntityUid attacker,
        EntityUid target,
        EntityUid weapon,
        bool allowCleave = true,
        bool allowNick = true,
        int depth = 0,
        DndWeaponProfile? profileOverride = null,
        bool pipeline = true)
    {
        if (!TryComp<DndAbilitiesComponent>(attacker, out var abilities))
            return new DamageSpecifier();

        if (!_allegiance.CanHarm(attacker, target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-attack-party"), attacker, attacker, PopupType.SmallCaution);
            return new DamageSpecifier();
        }

        var profile = profileOverride ?? Profile(weapon, attacker);
        if (profile.Ranged && !InRange(attacker, target, profile.RangeFeet))
        {
            _popup.PopupEntity(Loc.GetString("dnd-attack-range"), attacker, PopupType.Small);
            return new DamageSpecifier();
        }

        if (TryComp<DndSanctuaryComponent>(attacker, out var broken) && _timing.CurTime < broken.Until)
            RemComp<DndSanctuaryComponent>(attacker);

        if (TryComp<DndSanctuaryComponent>(target, out var sanctuary) && _timing.CurTime < sanctuary.Until && !WisdomSave(attacker, sanctuary.DifficultyClass))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-sanctuary"), attacker, attacker, PopupType.SmallCaution);
            return new DamageSpecifier();
        }

        var score = DndCombat.AttackScore(profile.Ability, abilities.Strength, abilities.Dexterity);
        var diceSides = profile.DiceSides;
        if (TryComp<DndShillelaghComponent>(attacker, out var shillelagh)
            && _timing.CurTime < shillelagh.Until
            && (profile.Kind == "Club" || profile.Kind == "Quarterstaff"))
        {
            score = abilities.Wisdom;
            if (profile.DiceCount > 0 && diceSides < 8)
                diceSides = 8;
        }

        var trained = Proficient(profile, CompOrNull<DndTrainingComponent>(attacker));
        var bonus = DndAbilityScores.Modifier(score);
        if (trained)
            bonus += abilities.ProficiencyBonus;

        bonus += DndSpellBuffs.RollDie(attacker, EntityManager, _timing, _random);

        var advantage = false;
        var disadvantage = false;
        if (HasComp<KnockedDownComponent>(attacker))
            disadvantage = true;

        if (HasComp<KnockedDownComponent>(target) && InRange(attacker, target, 5))
            advantage = true;

        if (_armor.HasPenalty(attacker))
            disadvantage = true;

        if (TryComp<DndSapComponent>(attacker, out var sap) && _timing.CurTime < sap.Until)
        {
            disadvantage = true;
            RemComp<DndSapComponent>(attacker);
        }

        if (TryComp<DndWeaponMasteryComponent>(attacker, out var mastery)
            && mastery.VexTarget == target
            && _timing.CurTime < mastery.VexUntil)
        {
            advantage = true;
            mastery.VexTarget = null;
        }

        if (TryComp<DndFaerieFireComponent>(target, out var fire) && _timing.CurTime < fire.Until)
            advantage = true;

        if (TryComp<DndBaneComponent>(attacker, out var bane) && _timing.CurTime < bane.Until)
            disadvantage = true;

        if (TryComp<DndBlindComponent>(attacker, out var blind) && _timing.CurTime < blind.Until)
            disadvantage = true;

        if (TryComp<DndProtectionComponent>(target, out var protection) && _timing.CurTime < protection.Until && _allegiance.IsOpposition(attacker, target))
            disadvantage = true;

        var first = _random.Next(1, DndCombat.DieSides + 1);
        var second = first;
        if (advantage != disadvantage)
            second = _random.Next(1, DndCombat.DieSides + 1);

        var roll = DndChecks.KeptRoll(first, second, advantage, disadvantage);
        var armorClass = _armor.ArmorClass(target, null);
        var outcome = DndCombat.ResolveAttack(roll, bonus, armorClass);
        var bonusText = DndAbilityScores.FormatSigned(bonus);
        var total = roll + bonus;
        if (outcome == DndAttackOutcome.Miss && roll != DndCombat.NaturalMiss)
        {
            total = _inspiration.Improve(attacker, roll, bonus, armorClass);
            if (total >= armorClass)
                outcome = DndAttackOutcome.Hit;
        }
        var specifier = new DamageSpecifier();

        if (outcome == DndAttackOutcome.Miss)
        {
            var miss = roll == DndCombat.NaturalMiss ? "dnd-attack-miss-natural" : "dnd-attack-miss";
            _popup.PopupEntity(Loc.GetString(miss, ("roll", roll), ("bonus", bonusText), ("total", total), ("ac", armorClass)), target, PopupType.Small);
        }
        else
        {
            var rageBonus = 0;
            if (TryComp<DndRageComponent>(attacker, out var rage)
                && rage.Active
                && DndRageRules.UsesStrength(profile.Ability, abilities.Strength, abilities.Dexterity))
                rageBonus = rage.DamageBonus;

            Reduction(target, profile.DamageType, out var resistant, out var vulnerable, out var immune);
            var dice = DndCombat.DamageDiceCount(profile.DiceCount, outcome == DndAttackOutcome.CriticalHit);
            var rolled = dice == 0 ? profile.FixedDamage : RollDice(dice, diceSides);
            var damage = DndCombat.ApplyDamage(
                rolled,
                DndAbilityScores.Modifier(score),
                profile.AddAbilityModifier,
                rageBonus,
                resistant,
                vulnerable,
                immune);

            var text = outcome == DndAttackOutcome.CriticalHit ? "dnd-attack-critical" : "dnd-attack-hit";
            _popup.PopupEntity(Loc.GetString(
                text,
                ("roll", roll),
                ("bonus", bonusText),
                ("total", total),
                ("ac", armorClass),
                ("damage", damage),
                ("type", Loc.GetString(DndDamageTypes.NameId(profile.DamageType)))), target, PopupType.Medium);

            specifier = Specifier(profile.DamageType, damage);
        }

        if (_net.IsServer)
        {
            var resolved = new DndAttackResolvedEvent
            {
                Attacker = attacker,
                Target = target,
                Weapon = weapon,
                Profile = profile,
                AbilityScore = score,
                Outcome = outcome,
                AllowCleave = allowCleave,
                AllowNick = allowNick,
                Depth = depth,
                Damage = specifier,
            };
            RaiseLocalEvent(attacker, ref resolved);
            specifier = resolved.Damage;
        }

        if (pipeline && _net.IsServer && !specifier.Empty)
            EnsureComp<DndAttackDamageResolvedComponent>(target);

        return specifier;
    }

    public void ReductionOf(EntityUid target, DndDamageType type, out bool resistant, out bool vulnerable, out bool immune)
    {
        Reduction(target, type, out resistant, out vulnerable, out immune);
    }

    public DamageSpecifier TypeDamage(DndDamageType type, int amount)
    {
        if (amount <= 0)
            return new DamageSpecifier();

        return Specifier(type, amount);
    }

    private static bool Proficient(DndWeaponProfile profile, DndTrainingComponent? training)
    {
        if (profile.Improvised)
            return training == null;

        if (profile.Training is not { } category)
            return true;

        if (training == null)
            return true;

        foreach (var trained in training.Weapons)
        {
            if (trained == category)
                return true;
        }

        return false;
    }

    private DndWeaponProfile Profile(EntityUid weapon, EntityUid attacker)
    {
        if (TryComp<DndWeaponComponent>(weapon, out var equipped))
            return DndWeaponProfile.From(equipped);

        if (TryComp<DndWeaponComponent>(attacker, out var unarmed))
            return DndWeaponProfile.From(unarmed);

        return DndWeaponProfile.Unarmed;
    }

    private bool InRange(EntityUid attacker, EntityUid target, int rangeFeet)
    {
        var delta = _transform.GetWorldPosition(attacker) - _transform.GetWorldPosition(target);
        return delta.Length() <= DndCombat.Tiles(rangeFeet) + 1f;
    }

    private void Reduction(EntityUid target, DndDamageType type, out bool resistant, out bool vulnerable, out bool immune)
    {
        resistant = false;
        vulnerable = false;
        immune = false;
        if (TryComp<DndRageComponent>(target, out var rage) && rage.Active && DndRageRules.IsPhysical(type))
            resistant = true;

        if (TryComp<DndWardComponent>(target, out var ward) && _timing.CurTime < ward.Until && DndRageRules.IsPhysical(type))
            resistant = true;

        if (!TryComp<DndDamageReductionComponent>(target, out var reduction))
            return;

        resistant = resistant || reduction.ResistAll || DndCombat.Listed(reduction.Resistances, type);
        vulnerable = reduction.VulnerableAll || DndCombat.Listed(reduction.Vulnerabilities, type);
        immune = reduction.ImmuneAll || DndCombat.Listed(reduction.Immunities, type);
    }

    private bool WisdomSave(EntityUid defender, int difficultyClass)
    {
        var score = 10;
        var proficiency = 0;
        var proficient = false;
        if (TryComp<DndAbilitiesComponent>(defender, out var abilities))
        {
            score = abilities.Wisdom;
            proficiency = abilities.ProficiencyBonus;
        }

        if (TryComp<DndTrainingComponent>(defender, out var training))
        {
            foreach (var save in training.SavingThrows)
            {
                if (save.Id == DndAbilityPrototype.WisdomId)
                {
                    proficient = true;
                    break;
                }
            }
        }

        var roll = _random.Next(1, DndCombat.DieSides + 1);
        var bonus = DndChecks.Bonus(score, proficient, proficiency) + DndSpellBuffs.RollDie(defender, EntityManager, _timing, _random);
        return roll + bonus >= difficultyClass;
    }

    private int RollDice(int count, int sides)
    {
        if (count < 1 || sides < 1)
            return 0;

        var total = 0;
        for (var i = 0; i < count; i++)
            total += _random.Next(1, sides + 1);

        return total;
    }

    private DamageSpecifier Specifier(DndDamageType type, int amount)
    {
        var id = DndDamageTypes.Engine(type);
        if (!_prototypes.TryIndex<DamageTypePrototype>(id, out var prototype))
            return new DamageSpecifier();

        return new DamageSpecifier(prototype, FixedPoint2.New(amount));
    }

    private int Constitution(HumanoidCharacterProfile profile)
    {
        var baseScore = profile.GetAbilityScore(DndAbilityPrototype.ConstitutionId, 10);
        if (!_prototypes.TryIndex(profile.DndBackground, out DndBackgroundPrototype? background))
            return baseScore;

        var bonus = DndBackgroundScores.Bonus(
            DndAbilityPrototype.ConstitutionId,
            background.Abilities,
            profile.BackgroundIncrease,
            profile.BackgroundPlusTwo.Id,
            profile.BackgroundPlusOne.Id);
        return DndBackgroundScores.FinalScore(baseScore, bonus);
    }
}
