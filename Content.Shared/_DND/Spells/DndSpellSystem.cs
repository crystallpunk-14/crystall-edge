using System.Numerics;
using Content.Shared._DND.Abilities;
using Content.Shared._DND.Classes;
using Content.Shared._DND.Combat;
using Content.Shared._DND.Equipment;
using Content.Shared._DND.Factions;
using Content.Shared._DND.Features;
using Content.Shared.Actions;
using Content.Shared.Charges.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._DND.Spells;

/// <summary>
/// Casts prepared bard spells. Cantrips spend no slot. Leveled spells share one pool, regained on a long rest.
/// </summary>
public sealed partial class DndSpellSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DndAllegianceSystem _allegiance = default!;
    [Dependency] private DndArmorSystem _armor = default!;
    [Dependency] private DndCombatSystem _combat = default!;
    [Dependency] private DndInspirationSystem _inspiration = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private MobStateSystem _mobs = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    public const float LongstriderSpeed = 40f / 30f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (!_net.IsServer)
            return;

        var now = _timing.CurTime;
        Expire<DndFaerieFireComponent>(now, static fire => fire.Until);
        Expire<DndBaneComponent>(now, static bane => bane.Until);
        Expire<DndBlindComponent>(now, static blind => blind.Until);
        Expire<DndShillelaghComponent>(now, static shillelagh => shillelagh.Until);
        Expire<DndSpeakWithAnimalsComponent>(now, static speak => speak.Until);
        Expire<DndLongstriderComponent>(now, static stride => stride.Until, uid => _speed.RefreshMovementSpeedModifiers(uid));
        Expire<DndTemporaryHitPointsComponent>(now, static temp => temp.Until);
        var charms = EntityQueryEnumerator<DndCharmComponent>();
        while (charms.MoveNext(out var uid, out var charm))
        {
            if (now < charm.Until)
                continue;

            RemComp<DndCharmComponent>(uid);
            RemComp<DndAllegianceShiftComponent>(uid);
        }

        var lights = EntityQueryEnumerator<DndDancingLightComponent>();
        while (lights.MoveNext(out var uid, out var light))
        {
            if (now >= light.ExpiresAt)
                QueueDel(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnCast(EntityUid uid, DndSpellcastingComponent component, DndCastSpellEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!_prototypes.TryIndex<DndSpellPrototype>(args.Spell, out var spell))
            return;

        if (spell.RequiresFocus && !HasFocus(uid, component))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-focus"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (spell.Level > 0 && component.Slots <= 0)
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-slots"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (!Cast(uid, args.Target, component, spell))
            return;

        if (spell.Level > 0)
        {
            component.Slots = DndBardRules.SlotsAfterCast(spell.Level, component.Slots);
            Dirty(uid, component);
            SyncSlots(component);
            if (args.Action.Owner.IsValid())
                _charges.AddCharges(args.Action.Owner, 1);
        }

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnMove(EntityUid uid, DndLongstriderComponent component, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_timing.CurTime >= component.Until)
            return;

        args.ModifySpeed(LongstriderSpeed);
    }

    public void RecoverLongRest(EntityUid uid, DndSpellcastingComponent casting)
    {
        casting.Slots = casting.MaxSlots;
        if (casting.ReprepareOnLongRest)
            casting.ReprepareUntil = _timing.CurTime + TimeSpan.FromMinutes(1);

        Dirty(uid, casting);
        SyncSlots(casting);
    }

    public void AddPrepareVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<DndSpellcastingComponent>(args.User, out var casting))
            return;

        if (!casting.ReprepareOnLongRest || _timing.CurTime > casting.ReprepareUntil)
            return;

        foreach (var prepared in casting.Prepared)
        {
            if (HasSpell(casting.AlwaysPrepared, prepared.Id))
                continue;

            if (!_prototypes.TryIndex(prepared, out DndSpellPrototype? spell))
                continue;

            var id = spell.ID;
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("dnd-spell-set-aside", ("spell", Loc.GetString(spell.Name))),
                Act = () => SetPrepared(args.User, id, false),
                Priority = 4,
            });
        }

        if (casting.Prepared.Count >= casting.MaxPrepared)
            return;

        foreach (var spell in _prototypes.EnumeratePrototypes<DndSpellPrototype>())
        {
            if (spell.Level != 1 || !Listed(spell.Lists, casting.SpellList) || HasSpell(casting.Prepared, spell.ID) || HasSpell(casting.AlwaysPrepared, spell.ID))
                continue;

            var id = spell.ID;
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("dnd-spell-prepare", ("spell", Loc.GetString(spell.Name))),
                Act = () => SetPrepared(args.User, id, true),
                Priority = 3,
            });
        }
    }

    [SubscribeLocalEvent]
    private void OnPrepareVerbs(EntityUid uid, DndSpellcastingComponent component, GetVerbsEvent<Verb> args)
    {
        if (args.User != uid)
            return;

        AddPrepareVerbs(args);
    }

    public void AbsorbTemporaryHitPoints(EntityUid uid, DamageSpecifier damage)
    {
        if (!TryComp<DndTemporaryHitPointsComponent>(uid, out var temp) || temp.Amount <= 0)
            return;

        var left = temp.Amount;
        var keys = new List<ProtoId<DamageTypePrototype>>();
        foreach (var (type, value) in damage.DamageDict)
        {
            if (value > 0)
                keys.Add(type);
        }

        foreach (var type in keys)
        {
            if (left <= 0)
                break;

            var value = damage.DamageDict[type];
            var take = Math.Min(left, (int) value.Float());
            if (take <= 0)
                continue;

            damage.DamageDict[type] = value - take;
            left -= take;
        }

        temp.Amount = left;
        if (temp.Amount <= 0)
            RemComp<DndTemporaryHitPointsComponent>(uid);
        else
            Dirty(uid, temp);
    }

    private bool Cast(EntityUid caster, EntityUid target, DndSpellcastingComponent casting, DndSpellPrototype spell)
    {
        switch (spell.Effect)
        {
            case DndSpellEffect.Mockery:
                return Harm(caster, target, () =>
                {
                    Damage(caster, target, spell, CastingModifier(caster, casting), save: false, half: false);
                    var sap = EnsureComp<DndSapComponent>(target);
                    sap.Until = _timing.CurTime + TimeSpan.FromSeconds(DndRageRules.RoundSeconds);
                    _popup.PopupEntity(Loc.GetString("dnd-spell-mockery"), target, PopupType.Small);
                });
            case DndSpellEffect.Lights:
                return SpawnLight(caster, target);
            case DndSpellEffect.Attack:
                return Harm(caster, target, () => SpellAttack(caster, target, casting, spell));
            case DndSpellEffect.SaveDamage:
                return Area(caster, target, spell.RangeFeet, cone: false, candidate =>
                    Damage(caster, candidate, spell, CastingModifier(caster, casting), save: true, spell.HalfOnSave));
            case DndSpellEffect.Heal:
                return Heal(caster, target, spell, CastingModifier(caster, casting));
            case DndSpellEffect.Charm:
                return Harm(caster, target, () => Charm(caster, target, casting));
            case DndSpellEffect.ConeBlind:
                return Area(caster, target, spell.RangeFeet, cone: true, candidate =>
                {
                    if (!_allegiance.CanHarm(caster, candidate))
                        return;
                    Blind(candidate, TimeSpan.FromSeconds(DndRageRules.RoundSeconds));
                });
            case DndSpellEffect.Whisper:
                return Harm(caster, target, () =>
                {
                    var failed = Damage(caster, target, spell, CastingModifier(caster, casting), save: true, spell.HalfOnSave);
                    if (failed)
                        Push(caster, target, 30);
                });
            case DndSpellEffect.Thunderwave:
                return Area(caster, target, spell.RangeFeet, cone: true, candidate =>
                {
                    var failed = Damage(caster, candidate, spell, CastingModifier(caster, casting), save: true, spell.HalfOnSave);
                    if (failed)
                        Push(caster, candidate, 10);
                });
            case DndSpellEffect.FaerieFire:
                return Area(target, target, 20, cone: false, candidate =>
                {
                    if (candidate != target && !_allegiance.CanHarm(caster, candidate))
                        return;
                    if (SaveSucceeds(caster, candidate, casting, spell.Save))
                        return;
                    var fire = EnsureComp<DndFaerieFireComponent>(candidate);
                    fire.Until = _timing.CurTime + TimeSpan.FromMinutes(1);
                    Dirty(candidate, fire);
                    _popup.PopupEntity(Loc.GetString("dnd-spell-faerie-fire"), candidate, PopupType.Small);
                });
            case DndSpellEffect.Bane:
                return Harm(caster, target, () =>
                {
                    if (SaveSucceeds(caster, target, casting, spell.Save))
                    {
                        _popup.PopupEntity(Loc.GetString("dnd-spell-save"), target, PopupType.Small);
                        return;
                    }

                    var bane = EnsureComp<DndBaneComponent>(target);
                    bane.Until = _timing.CurTime + TimeSpan.FromMinutes(1);
                    Dirty(target, bane);
                    _popup.PopupEntity(Loc.GetString("dnd-spell-bane"), target, PopupType.Small);
                });
            case DndSpellEffect.Heroism:
                return GrantHeroism(caster, target, casting);
            case DndSpellEffect.Sleep:
                return Sleep(caster, target);
            case DndSpellEffect.Longstrider:
                return GrantLongstrider(target);
            case DndSpellEffect.Guidance:
                return GrantTimed(caster, target, () =>
                {
                    var guidance = EnsureComp<DndGuidanceComponent>(target);
                    guidance.Until = _timing.CurTime + TimeSpan.FromMinutes(1);
                    Dirty(target, guidance);
                }, "dnd-spell-guidance");
            case DndSpellEffect.Smite:
                return Harm(caster, target, () => Damage(caster, target, spell, CastingModifier(caster, casting), save: true, spell.HalfOnSave));
            case DndSpellEffect.Mend:
                return Mend(target);
            case DndSpellEffect.Ward:
                return GrantTimed(caster, target, () =>
                {
                    var ward = EnsureComp<DndWardComponent>(target);
                    ward.Until = _timing.CurTime + TimeSpan.FromMinutes(1);
                    Dirty(target, ward);
                }, "dnd-spell-ward");
            case DndSpellEffect.SpareDying:
                return SpareDying(caster, target);
            case DndSpellEffect.Wonder:
                SpawnLight(caster, target);
                _popup.PopupEntity(Loc.GetString("dnd-spell-wonder"), target, PopupType.Small);
                return true;
            case DndSpellEffect.Bless:
                return Bless(caster, target);
            case DndSpellEffect.Command:
                return Harm(caster, target, () =>
                {
                    if (SaveSucceeds(caster, target, casting, DndAbilityPrototype.WisdomId))
                    {
                        _popup.PopupEntity(Loc.GetString("dnd-spell-save"), target, PopupType.Small);
                        return;
                    }

                    _stun.TryKnockdown(target, TimeSpan.FromSeconds(DndRageRules.RoundSeconds), refresh: true, autoStand: false, drop: false);
                    _popup.PopupEntity(Loc.GetString("dnd-spell-command"), target, PopupType.Small);
                });
            case DndSpellEffect.GuidingBolt:
                return Harm(caster, target, () =>
                {
                    if (!SpellAttack(caster, target, casting, spell))
                        return;

                    var mark = EnsureComp<DndFaerieFireComponent>(target);
                    mark.Until = _timing.CurTime + TimeSpan.FromSeconds(DndRageRules.RoundSeconds);
                    Dirty(target, mark);
                });
            case DndSpellEffect.ShieldOfFaith:
                return GrantTimed(caster, target, () =>
                {
                    var faith = EnsureComp<DndShieldOfFaithComponent>(target);
                    faith.Until = _timing.CurTime + TimeSpan.FromMinutes(1);
                    Dirty(target, faith);
                }, "dnd-spell-shield-of-faith");
            case DndSpellEffect.Sanctuary:
                return Sanctuary(caster, target, casting);
            case DndSpellEffect.Protection:
                return Protection(caster, target);
            case DndSpellEffect.DetectSides:
                return DetectSides(caster, target);
            case DndSpellEffect.DetectMagic:
                return DetectMagic(caster, target);
            case DndSpellEffect.DetectPoison:
                return DetectPoison(caster, target);
            case DndSpellEffect.Splash:
                return Splash(caster, target);
            case DndSpellEffect.Purify:
                return Purify(target);
            case DndSpellEffect.Druidcraft:
                return Druidcraft(caster);
            case DndSpellEffect.Elementalism:
                return Elementalism(caster, target);
            case DndSpellEffect.Message:
                return WhisperMessage(caster, target);
            case DndSpellEffect.ProduceFlame:
                return ProduceFlame(caster, target, casting, spell);
            case DndSpellEffect.Shillelagh:
                return Shillelagh(caster);
            case DndSpellEffect.AnimalFriendship:
                return AnimalFriendship(caster, target, casting);
            case DndSpellEffect.Entangle:
                return Entangle(caster, target, casting);
            case DndSpellEffect.Fog:
                return Fog(target);
            case DndSpellEffect.Goodberry:
                return Goodberry(caster);
            case DndSpellEffect.IceKnife:
                return IceKnife(caster, target, casting, spell);
            case DndSpellEffect.Jump:
                return Jump(caster, target);
            case DndSpellEffect.SpeakWithAnimals:
                return SpeakWithAnimals(caster);
            default:
                return false;
        }
    }

    private bool Harm(EntityUid caster, EntityUid target, Action effect)
    {
        if (!Alive(target) || !_allegiance.CanHarm(caster, target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        effect();
        return true;
    }

    private bool Area(EntityUid caster, EntityUid aim, int feet, bool cone, Action<EntityUid> effect)
    {
        var hit = false;
        var origin = _transform.GetWorldPosition(caster);
        var toward = _transform.GetWorldPosition(aim) - origin;
        foreach (var candidate in _lookup.GetEntitiesInRange(Transform(caster).Coordinates, DndCombat.Tiles(feet), LookupFlags.Uncontained))
        {
            if (candidate == caster || !Alive(candidate))
                continue;

            if (cone && toward.LengthSquared() > 0.01f)
            {
                var delta = _transform.GetWorldPosition(candidate) - origin;
                if (delta.LengthSquared() < 0.01f || Vector2.Dot(Vector2.Normalize(delta), Vector2.Normalize(toward)) < 0.5f)
                    continue;
            }

            effect(candidate);
            hit = true;
        }

        if (!hit)
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);

        return hit;
    }

    /// <returns>True when the target fails the save, or when there is no save.</returns>
    private bool Damage(EntityUid caster, EntityUid target, DndSpellPrototype spell, int modifier, bool save, bool half)
    {
        if (!_allegiance.CanHarm(caster, target))
            return false;

        var failed = true;
        if (save && spell.Save != "")
            failed = !SaveSucceeds(caster, target, CompOrNull<DndSpellcastingComponent>(caster), spell.Save);

        var dice = spell.DamageDice;
        var rolled = Roll(dice, spell.DamageSides);
        if (spell.AddCastingModifier)
            rolled += modifier;

        if (!failed)
        {
            if (!half)
            {
                _popup.PopupEntity(Loc.GetString("dnd-spell-save"), target, PopupType.Small);
                return false;
            }

            rolled = Math.Max(0, rolled / 2);
        }

        rolled = Math.Max(0, rolled);
        _combat.ReductionOf(target, spell.DamageType, out var resistant, out var vulnerable, out var immune);
        var damage = DndCombat.ApplyDamage(rolled, 0, false, 0, resistant, vulnerable, immune);
        if (damage > 0)
            _damageable.TryChangeDamage(target, _combat.TypeDamage(spell.DamageType, damage), origin: caster, ignoreResistances: true);

        _popup.PopupEntity(Loc.GetString("dnd-spell-damage", ("damage", damage), ("type", Loc.GetString(DndDamageTypes.NameId(spell.DamageType)))), target, PopupType.Small);
        return failed;
    }

    private bool SpellAttack(EntityUid caster, EntityUid target, DndSpellcastingComponent casting, DndSpellPrototype spell)
    {
        if (!TryComp<DndAbilitiesComponent>(caster, out var abilities))
            return false;

        var bonus = DndAbilityScores.Modifier(abilities.GetScore(casting.Ability)) + abilities.ProficiencyBonus + DndSpellBuffs.RollDie(caster, EntityManager, _timing, _random);
        var roll = _random.Next(1, DndCombat.DieSides + 1);
        var armorClass = _armor.ArmorClass(target, null);
        var total = roll + bonus;
        var outcome = DndCombat.ResolveAttack(roll, bonus, armorClass);
        if (outcome == DndAttackOutcome.Miss && roll != DndCombat.NaturalMiss)
        {
            total = _inspiration.Improve(caster, roll, bonus, armorClass);
            if (total >= armorClass)
                outcome = DndAttackOutcome.Hit;
        }

        if (outcome == DndAttackOutcome.Miss)
        {
            _popup.PopupEntity(Loc.GetString("dnd-attack-miss", ("roll", roll), ("bonus", DndAbilityScores.FormatSigned(bonus)), ("total", total), ("ac", armorClass)), target, PopupType.Small);
            return false;
        }

        var dice = DndCombat.DamageDiceCount(spell.DamageDice, outcome == DndAttackOutcome.CriticalHit);
        var rolled = Roll(dice, spell.DamageSides);
        _combat.ReductionOf(target, spell.DamageType, out var resistant, out var vulnerable, out var immune);
        var damage = DndCombat.ApplyDamage(rolled, 0, false, 0, resistant, vulnerable, immune);
        if (damage > 0)
            _damageable.TryChangeDamage(target, _combat.TypeDamage(spell.DamageType, damage), origin: caster, ignoreResistances: true);

        _popup.PopupEntity(Loc.GetString("dnd-spell-damage", ("damage", damage), ("type", Loc.GetString(DndDamageTypes.NameId(spell.DamageType)))), target, PopupType.Medium);
        return true;
    }

    private bool Heal(EntityUid caster, EntityUid target, DndSpellPrototype spell, int modifier)
    {
        if (!Alive(target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        var amount = Roll(spell.HealDice, spell.HealSides);
        if (spell.AddCastingModifier)
            amount += Math.Max(0, modifier);

        amount = Math.Max(1, amount);
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return true;

        var existing = _damageable.GetAllDamage(target);
        var left = amount;
        var heal = new DamageSpecifier();
        foreach (var (type, value) in existing.DamageDict)
        {
            if (left <= 0 || value <= 0)
                continue;

            var chunk = Math.Min(left, (int) value.Float());
            heal.DamageDict[type] = -chunk;
            left -= chunk;
        }

        if (heal.DamageDict.Count > 0)
            _damageable.TryChangeDamage(target, heal, ignoreResistances: true, origin: caster);

        _popup.PopupEntity(Loc.GetString("dnd-spell-heal", ("amount", amount)), target, PopupType.Medium);
        return true;
    }

    private void Charm(EntityUid caster, EntityUid target, DndSpellcastingComponent casting)
    {
        if (SaveSucceeds(caster, target, casting, DndAbilityPrototype.WisdomId))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-save"), target, PopupType.Small);
            return;
        }

        if (_allegiance.CurrentSide(caster) is not { } side)
            return;

        var shift = EnsureComp<DndAllegianceShiftComponent>(target);
        shift.Side = side;
        Dirty(target, shift);
        var charm = EnsureComp<DndCharmComponent>(target);
        charm.Until = _timing.CurTime + TimeSpan.FromHours(1);
        Dirty(target, charm);
        _popup.PopupEntity(Loc.GetString("dnd-spell-charm"), target, PopupType.Medium);
    }

    private bool GrantHeroism(EntityUid caster, EntityUid target, DndSpellcastingComponent casting)
    {
        if (!Alive(target))
            return false;

        var amount = Math.Max(1, CastingModifier(caster, casting));
        var temp = EnsureComp<DndTemporaryHitPointsComponent>(target);
        temp.Amount = Math.Max(temp.Amount, amount);
        temp.Until = _timing.CurTime + TimeSpan.FromMinutes(1);
        Dirty(target, temp);
        _popup.PopupEntity(Loc.GetString("dnd-spell-heroism", ("amount", temp.Amount)), target, PopupType.Medium);
        return true;
    }

    private bool Sleep(EntityUid caster, EntityUid point)
    {
        var pool = Roll(5, 8);
        var victims = new List<(EntityUid uid, int hp)>();
        foreach (var candidate in _lookup.GetEntitiesInRange(Transform(point).Coordinates, DndCombat.Tiles(20), LookupFlags.Uncontained))
        {
            if (!Alive(candidate) || !_allegiance.CanHarm(caster, candidate))
                continue;

            victims.Add((candidate, Remaining(candidate)));
        }

        victims.Sort((a, b) => a.hp.CompareTo(b.hp));
        var slept = false;
        foreach (var (uid, hp) in victims)
        {
            if (hp > pool)
                continue;

            pool -= hp;
            _stun.TryKnockdown(uid, TimeSpan.FromMinutes(1), refresh: true, autoStand: false, drop: true);
            _popup.PopupEntity(Loc.GetString("dnd-spell-sleep"), uid, PopupType.Medium);
            slept = true;
        }

        if (!slept)
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);

        return slept;
    }

    private bool GrantLongstrider(EntityUid target)
    {
        if (!Alive(target))
            return false;

        var stride = EnsureComp<DndLongstriderComponent>(target);
        stride.Until = _timing.CurTime + TimeSpan.FromHours(1);
        Dirty(target, stride);
        _speed.RefreshMovementSpeedModifiers(target);
        _popup.PopupEntity(Loc.GetString("dnd-spell-longstrider"), target, PopupType.Medium);
        return true;
    }

    private bool SpawnLight(EntityUid caster, EntityUid target)
    {
        var prototype = "DndDancingLight";
        var light = Spawn(prototype, Transform(target).Coordinates);
        var timed = EnsureComp<DndDancingLightComponent>(light);
        timed.ExpiresAt = _timing.CurTime + TimeSpan.FromMinutes(10);
        _popup.PopupEntity(Loc.GetString("dnd-spell-lights"), caster, caster, PopupType.Small);
        return true;
    }

    private void Blind(EntityUid target, TimeSpan duration)
    {
        var blind = EnsureComp<DndBlindComponent>(target);
        var until = _timing.CurTime + duration;
        if (until > blind.Until)
            blind.Until = until;

        Dirty(target, blind);
        _popup.PopupEntity(Loc.GetString("dnd-spell-blind"), target, PopupType.Small);
    }

    private bool SaveSucceeds(EntityUid caster, EntityUid target, DndSpellcastingComponent? casting, string abilityId)
    {
        if (casting == null || abilityId == "")
            return false;

        var score = 10;
        var proficiency = 0;
        if (TryComp<DndAbilitiesComponent>(target, out var abilities))
        {
            score = abilities.GetScore(abilityId);
            proficiency = abilities.ProficiencyBonus;
        }

        var proficient = false;
        if (TryComp<DndTrainingComponent>(target, out var training))
        {
            foreach (var save in training.SavingThrows)
            {
                if (save.Id == abilityId)
                    proficient = true;
            }
        }

        var disadvantage = TryComp<DndBaneComponent>(target, out var bane) && _timing.CurTime < bane.Until;
        var first = _random.Next(1, DndCombat.DieSides + 1);
        var second = first;
        if (disadvantage)
            second = _random.Next(1, DndCombat.DieSides + 1);

        var roll = DndChecks.KeptRoll(first, second, advantage: false, disadvantage);
        var bonus = DndChecks.Bonus(score, proficient, proficiency);
        var dc = SpellDc(caster, casting);
        var total = _inspiration.Improve(target, roll, bonus, dc);
        return total >= dc;
    }

    private int SpellDc(EntityUid caster, DndSpellcastingComponent casting)
    {
        if (!TryComp<DndAbilitiesComponent>(caster, out var abilities))
            return 8;

        return DndChecks.DifficultyClass(abilities.GetScore(casting.Ability), abilities.ProficiencyBonus);
    }

    private int CastingModifier(EntityUid caster, DndSpellcastingComponent casting)
    {
        if (!TryComp<DndAbilitiesComponent>(caster, out var abilities))
            return 0;

        return DndAbilityScores.Modifier(abilities.GetScore(casting.Ability));
    }

    private void Push(EntityUid caster, EntityUid target, int feet)
    {
        var delta = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(caster);
        if (delta.LengthSquared() < 0.01f)
            delta = new Vector2(1f, 0f);

        _throwing.TryThrow(target, Vector2.Normalize(delta) * DndCombat.Tiles(feet), user: caster, pushbackRatio: 0f, compensateFriction: true, recoil: false, doSpin: false, playSound: false);
    }

    private void SyncSlots(DndSpellcastingComponent casting)
    {
        foreach (var action in casting.SlotActions)
        {
            if (!Exists(action))
                continue;

            _charges.SetMaxCharges(action, casting.MaxSlots);
            _charges.SetCharges(action, casting.Slots);
        }
    }

    private bool GrantTimed(EntityUid caster, EntityUid target, Action grant, string message)
    {
        if (!Willing(caster, target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        grant();
        _popup.PopupEntity(Loc.GetString(message), target, PopupType.Medium);
        return true;
    }

    private bool Willing(EntityUid caster, EntityUid target)
    {
        return Alive(target) && (caster == target || !_allegiance.CanHarm(caster, target));
    }

    private bool Bless(EntityUid caster, EntityUid target)
    {
        if (!Willing(caster, target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        var blessed = 0;
        BlessOne(target);
        blessed++;
        foreach (var candidate in _lookup.GetEntitiesInRange(Transform(target).Coordinates, DndCombat.Tiles(30), LookupFlags.Uncontained))
        {
            if (blessed >= 3 || candidate == target || !Willing(caster, candidate))
                continue;

            BlessOne(candidate);
            blessed++;
        }

        return true;
    }

    private void BlessOne(EntityUid target)
    {
        var bless = EnsureComp<DndBlessComponent>(target);
        bless.Until = _timing.CurTime + TimeSpan.FromMinutes(1);
        Dirty(target, bless);
        _popup.PopupEntity(Loc.GetString("dnd-spell-bless"), target, PopupType.Small);
    }

    private bool Sanctuary(EntityUid caster, EntityUid target, DndSpellcastingComponent casting)
    {
        if (!Willing(caster, target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        var ward = EnsureComp<DndSanctuaryComponent>(target);
        ward.Until = _timing.CurTime + TimeSpan.FromMinutes(1);
        ward.DifficultyClass = SpellDifficulty(caster, casting);
        Dirty(target, ward);
        _popup.PopupEntity(Loc.GetString("dnd-spell-sanctuary-ward"), target, PopupType.Medium);
        return true;
    }

    private bool Protection(EntityUid caster, EntityUid target)
    {
        if (!Willing(caster, target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        RemComp<DndCharmComponent>(target);
        RemComp<DndAllegianceShiftComponent>(target);
        var ward = EnsureComp<DndProtectionComponent>(target);
        ward.Until = _timing.CurTime + TimeSpan.FromMinutes(1);
        Dirty(target, ward);
        _popup.PopupEntity(Loc.GetString("dnd-spell-protection"), target, PopupType.Medium);
        return true;
    }

    private bool Mend(EntityUid target)
    {
        if (Alive(target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), target, PopupType.SmallCaution);
            return false;
        }

        var existing = _damageable.GetAllDamage(target);
        var heal = new DamageSpecifier();
        foreach (var (type, value) in existing.DamageDict)
        {
            if (value > 0)
                heal.DamageDict[type] = -value;
        }

        if (heal.DamageDict.Count > 0)
            _damageable.TryChangeDamage(target, heal, ignoreResistances: true);

        _popup.PopupEntity(Loc.GetString("dnd-spell-mend"), target, PopupType.Small);
        return true;
    }

    private bool SpareDying(EntityUid caster, EntityUid target)
    {
        if (!TryComp<MobStateComponent>(target, out var state) || state.CurrentState == MobState.Dead)
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        var dying = state.CurrentState == MobState.Critical || Remaining(target) <= 0;
        if (!dying)
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        if (state.CurrentState == MobState.Critical)
            _mobs.ChangeMobState(target, MobState.Alive, state);

        HealFlat(caster, target, 1);
        _popup.PopupEntity(Loc.GetString("dnd-spell-spare"), target, PopupType.Medium);
        return true;
    }

    private void HealFlat(EntityUid caster, EntityUid target, int amount)
    {
        var existing = _damageable.GetAllDamage(target);
        var left = amount;
        var heal = new DamageSpecifier();
        foreach (var (type, value) in existing.DamageDict)
        {
            if (left <= 0 || value <= 0)
                continue;

            var chunk = Math.Min(left, (int) value.Float());
            heal.DamageDict[type] = -chunk;
            left -= chunk;
        }

        if (heal.DamageDict.Count > 0)
            _damageable.TryChangeDamage(target, heal, ignoreResistances: true, origin: caster);
    }

    private bool DetectSides(EntityUid caster, EntityUid target)
    {
        var found = false;
        foreach (var candidate in _lookup.GetEntitiesInRange(Transform(target).Coordinates, DndCombat.Tiles(30), LookupFlags.Uncontained))
        {
            if (!Alive(candidate) || _allegiance.CurrentSide(candidate) is not { } side)
                continue;

            if (!_prototypes.TryIndex(side, out Content.Shared._DND.Factions.DndSidePrototype? proto))
                continue;

            found = true;
            _popup.PopupEntity(Loc.GetString("dnd-spell-detect-side", ("name", Name(candidate)), ("side", Loc.GetString(proto.Name))), caster, caster, PopupType.Small);
        }

        if (!found)
            _popup.PopupEntity(Loc.GetString("dnd-spell-detect-none"), caster, caster, PopupType.Small);

        return true;
    }

    private bool DetectMagic(EntityUid caster, EntityUid target)
    {
        var found = false;
        foreach (var candidate in _lookup.GetEntitiesInRange(Transform(target).Coordinates, DndCombat.Tiles(30), LookupFlags.Uncontained))
        {
            if (!HasComp<DndSpellcastingComponent>(candidate) && !HasComp<DndCharmComponent>(candidate) && !HasComp<DndFaerieFireComponent>(candidate))
                continue;

            found = true;
            _popup.PopupEntity(Loc.GetString("dnd-spell-detect-magic", ("name", Name(candidate))), caster, caster, PopupType.Small);
        }

        if (!found)
            _popup.PopupEntity(Loc.GetString("dnd-spell-detect-none"), caster, caster, PopupType.Small);

        return true;
    }

    private bool DetectPoison(EntityUid caster, EntityUid target)
    {
        var poisoned = false;
        var existing = _damageable.GetAllDamage(target);
        foreach (var (type, value) in existing.DamageDict)
        {
            if (value > 0 && type.Id == "Poison")
                poisoned = true;
        }

        _popup.PopupEntity(Loc.GetString(poisoned ? "dnd-spell-poison" : "dnd-spell-detect-none"), caster, caster, PopupType.Small);
        return true;
    }

    private bool Splash(EntityUid caster, EntityUid target)
    {
        if (Alive(target))
        {
            var slow = EnsureComp<DndSlowComponent>(target);
            slow.Until = _timing.CurTime + TimeSpan.FromSeconds(DndRageRules.RoundSeconds);
            Dirty(target, slow);
            _speed.RefreshMovementSpeedModifiers(target);
        }

        _popup.PopupEntity(Loc.GetString("dnd-spell-splash"), target, PopupType.Small);
        return true;
    }

    private bool Purify(EntityUid target)
    {
        if (Alive(target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), target, PopupType.SmallCaution);
            return false;
        }

        _popup.PopupEntity(Loc.GetString("dnd-spell-purify"), target, PopupType.Small);
        return true;
    }

    private bool Druidcraft(EntityUid caster)
    {
        var prototype = "DndDancingLight";
        var light = Spawn(prototype, Transform(caster).Coordinates);
        var timed = EnsureComp<DndDancingLightComponent>(light);
        timed.ExpiresAt = _timing.CurTime + TimeSpan.FromMinutes(10);
        _popup.PopupEntity(Loc.GetString("dnd-spell-druidcraft"), caster, PopupType.Small);
        return true;
    }

    private bool Elementalism(EntityUid caster, EntityUid target)
    {
        return Harm(caster, target, () =>
        {
            Push(caster, target, 5);
            _popup.PopupEntity(Loc.GetString("dnd-spell-elementalism"), target, PopupType.Small);
        });
    }

    private bool WhisperMessage(EntityUid caster, EntityUid target)
    {
        if (!Alive(target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private bool ProduceFlame(EntityUid caster, EntityUid target, DndSpellcastingComponent casting, DndSpellPrototype spell)
    {
        if (!Harm(caster, target, () => SpellAttack(caster, target, casting, spell)))
            return false;

        var prototype = "DndDancingLight";
        var light = Spawn(prototype, Transform(caster).Coordinates);
        var timed = EnsureComp<DndDancingLightComponent>(light);
        timed.ExpiresAt = _timing.CurTime + TimeSpan.FromMinutes(10);
        return true;
    }

    private bool Shillelagh(EntityUid caster)
    {
        if (!HoldingStaff(caster))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-shillelagh-weapon"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        var comp = EnsureComp<DndShillelaghComponent>(caster);
        comp.Until = _timing.CurTime + TimeSpan.FromMinutes(1);
        Dirty(caster, comp);
        _popup.PopupEntity(Loc.GetString("dnd-spell-shillelagh"), caster, PopupType.Small);
        return true;
    }

    private bool HoldingStaff(EntityUid caster)
    {
        if (!TryComp<HandsComponent>(caster, out var hands))
            return false;

        foreach (var held in _hands.EnumerateHeld((caster, hands)))
        {
            if (!TryComp<DndWeaponComponent>(held, out var weapon))
                continue;

            if (weapon.Kind == "Club" || weapon.Kind == "Quarterstaff")
                return true;
        }

        return false;
    }

    private bool AnimalFriendship(EntityUid caster, EntityUid target, DndSpellcastingComponent casting)
    {
        if (HasComp<DndClassComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-not-beast"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        return Harm(caster, target, () => Charm(caster, target, casting));
    }

    private bool Entangle(EntityUid caster, EntityUid point, DndSpellcastingComponent casting)
    {
        var hit = false;
        foreach (var candidate in _lookup.GetEntitiesInRange(Transform(point).Coordinates, DndCombat.Tiles(20), LookupFlags.Uncontained))
        {
            if (!Alive(candidate) || !_allegiance.CanHarm(caster, candidate))
                continue;

            hit = true;
            if (SaveSucceeds(caster, candidate, casting, DndAbilityPrototype.StrengthId))
            {
                _popup.PopupEntity(Loc.GetString("dnd-spell-save"), candidate, PopupType.Small);
                continue;
            }

            _stun.TryKnockdown(candidate, TimeSpan.FromSeconds(DndRageRules.RoundSeconds), refresh: true, autoStand: true, drop: false);
            _popup.PopupEntity(Loc.GetString("dnd-spell-entangle"), candidate, PopupType.Small);
        }

        if (!hit)
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);

        return hit;
    }

    private bool Fog(EntityUid point)
    {
        foreach (var candidate in _lookup.GetEntitiesInRange(Transform(point).Coordinates, DndCombat.Tiles(20), LookupFlags.Uncontained))
        {
            if (!Alive(candidate))
                continue;

            Blind(candidate, TimeSpan.FromSeconds(DndRageRules.RoundSeconds));
        }

        _popup.PopupEntity(Loc.GetString("dnd-spell-fog"), point, PopupType.Small);
        return true;
    }

    private bool Goodberry(EntityUid caster)
    {
        var prototype = "DndGoodberry";
        var berry = Spawn(prototype, Transform(caster).Coordinates);
        if (TryComp<HandsComponent>(caster, out var hands))
            _hands.TryPickupAnyHand(caster, berry, handsComp: hands);

        _popup.PopupEntity(Loc.GetString("dnd-spell-goodberry"), caster, PopupType.Small);
        return true;
    }

    [SubscribeLocalEvent]
    private void OnGoodberry(EntityUid uid, DndGoodberryComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("dnd-spell-eat-berry", ("uses", component.Uses)),
            Act = () => EatBerry(uid, user),
        });
    }

    private void EatBerry(EntityUid berry, EntityUid user)
    {
        if (!_net.IsServer || !TryComp<DndGoodberryComponent>(berry, out var component) || component.Uses <= 0)
            return;

        if (!Alive(user))
            return;

        component.Uses--;
        Dirty(berry, component);
        var amount = 1;
        if (TryComp<DamageableComponent>(user, out _))
        {
            var existing = _damageable.GetAllDamage(user);
            var heal = new DamageSpecifier();
            foreach (var (type, value) in existing.DamageDict)
            {
                if (value <= 0)
                    continue;

                heal.DamageDict[type] = -1;
                break;
            }

            if (heal.DamageDict.Count > 0)
                _damageable.TryChangeDamage(user, heal, ignoreResistances: true, origin: user);
        }

        _popup.PopupEntity(Loc.GetString("dnd-spell-heal", ("amount", amount)), user, PopupType.Small);
        if (component.Uses <= 0)
            QueueDel(berry);
    }

    private bool IceKnife(EntityUid caster, EntityUid target, DndSpellcastingComponent casting, DndSpellPrototype spell)
    {
        if (!Harm(caster, target, () => SpellAttack(caster, target, casting, spell)))
            return false;

        foreach (var candidate in _lookup.GetEntitiesInRange(Transform(target).Coordinates, DndCombat.Tiles(5), LookupFlags.Uncontained))
        {
            if (!Alive(candidate) || !_allegiance.CanHarm(caster, candidate))
                continue;

            var rolled = Roll(2, 6);
            if (SaveSucceeds(caster, candidate, casting, DndAbilityPrototype.ConstitutionId))
                rolled /= 2;

            _combat.ReductionOf(candidate, DndDamageType.Cold, out var resistant, out var vulnerable, out var immune);
            var damage = DndCombat.ApplyDamage(rolled, 0, false, 0, resistant, vulnerable, immune);
            if (damage > 0)
                _damageable.TryChangeDamage(candidate, _combat.TypeDamage(DndDamageType.Cold, damage), origin: caster, ignoreResistances: true);

            _popup.PopupEntity(Loc.GetString("dnd-spell-damage", ("damage", damage), ("type", Loc.GetString(DndDamageTypes.NameId(DndDamageType.Cold)))), candidate, PopupType.Small);
        }

        return true;
    }

    private bool Jump(EntityUid caster, EntityUid target)
    {
        if (!Willing(caster, target))
        {
            _popup.PopupEntity(Loc.GetString("dnd-spell-target"), caster, caster, PopupType.SmallCaution);
            return false;
        }

        var direction = _transform.GetWorldRotation(target).ToWorldVec();
        if (direction.LengthSquared() < 0.01f)
            direction = new Vector2(1f, 0f);

        _throwing.TryThrow(target, direction * 3f, user: caster, pushbackRatio: 0f, compensateFriction: true, recoil: false, doSpin: false, playSound: false);
        _popup.PopupEntity(Loc.GetString("dnd-spell-jump"), target, PopupType.Small);
        return true;
    }

    private bool SpeakWithAnimals(EntityUid caster)
    {
        var speak = EnsureComp<DndSpeakWithAnimalsComponent>(caster);
        speak.Until = _timing.CurTime + TimeSpan.FromMinutes(10);
        Dirty(caster, speak);
        _popup.PopupEntity(Loc.GetString("dnd-spell-speak-animals"), caster, PopupType.Small);
        return true;
    }

    private int SpellDifficulty(EntityUid caster, DndSpellcastingComponent casting)
    {
        if (!TryComp<DndAbilitiesComponent>(caster, out var abilities))
            return 10;

        return DndChecks.DifficultyClass(abilities.GetScore(casting.Ability), abilities.ProficiencyBonus);
    }

    private void SetPrepared(EntityUid uid, string spellId, bool prepare)
    {
        if (!_net.IsServer || !TryComp<DndSpellcastingComponent>(uid, out var casting))
            return;

        if (_timing.CurTime > casting.ReprepareUntil)
            return;

        if (!_prototypes.TryIndex<DndSpellPrototype>(spellId, out var spell) || spell.Level != 1 || !Listed(spell.Lists, casting.SpellList))
            return;

        if (HasSpell(casting.AlwaysPrepared, spellId))
            return;

        if (prepare)
        {
            if (HasSpell(casting.Prepared, spellId) || casting.Prepared.Count >= casting.MaxPrepared)
                return;

            casting.Prepared.Add(spellId);
            AddSpellAction(uid, casting, spell, slot: true);
        }
        else
        {
            RemovePrepared(uid, casting, spellId);
        }

        Dirty(uid, casting);
        SyncSlots(casting);
    }

    private void AddSpellAction(EntityUid uid, DndSpellcastingComponent casting, DndSpellPrototype spell, bool slot)
    {
        var actionId = "DndAction" + spell.ID;
        var action = _actions.AddAction(uid, actionId);
        if (action == null)
            return;

        var marked = EnsureComp<DndSpellActionComponent>(action.Value);
        marked.Spell = spell.ID;
        var delay = spell.Casting == DndCastingTime.BonusAction ? DndActionPace.BonusActionSeconds : DndActionPace.ActionSeconds;
        _actions.SetUseDelay(action.Value, TimeSpan.FromSeconds(delay));
        if (!slot)
            return;

        casting.SlotActions.Add(action.Value);
        _charges.SetMaxCharges(action.Value, casting.MaxSlots);
        _charges.SetCharges(action.Value, casting.Slots);
    }

    private void RemovePrepared(EntityUid uid, DndSpellcastingComponent casting, string spellId)
    {
        for (var i = casting.Prepared.Count - 1; i >= 0; i--)
        {
            if (casting.Prepared[i].Id == spellId)
                casting.Prepared.RemoveAt(i);
        }

        for (var i = casting.SlotActions.Count - 1; i >= 0; i--)
        {
            var action = casting.SlotActions[i];
            if (!TryComp<DndSpellActionComponent>(action, out var marked) || marked.Spell != spellId)
                continue;

            _actions.RemoveAction(action);
            casting.SlotActions.RemoveAt(i);
        }
    }

    private bool HasFocus(EntityUid uid, DndSpellcastingComponent casting)
    {
        if (casting.Focus == DndFocusKind.None)
            return false;

        var slots = _inventory.GetSlotEnumerator(uid);
        while (slots.MoveNext(out ContainerSlot? slot))
        {
            if (slot.ContainedEntity is { } worn && FocusMatches(worn, casting.Focus))
                return true;
        }

        if (!TryComp<HandsComponent>(uid, out var hands))
            return false;

        foreach (var held in _hands.EnumerateHeld((uid, hands)))
        {
            if (FocusMatches(held, casting.Focus))
                return true;
        }

        return false;
    }

    private bool FocusMatches(EntityUid item, DndFocusKind kind)
    {
        if (kind == DndFocusKind.Instrument)
            return HasComp<DndInstrumentComponent>(item);

        return TryComp<DndSpellFocusComponent>(item, out var focus) && focus.Kind == kind;
    }

    private static bool Listed(IReadOnlyList<string> ids, string id)
    {
        foreach (var entry in ids)
        {
            if (entry == id)
                return true;
        }

        return false;
    }

    private static bool HasSpell(IReadOnlyList<ProtoId<DndSpellPrototype>> ids, string id)
    {
        foreach (var entry in ids)
        {
            if (entry.Id == id)
                return true;
        }

        return false;
    }

    private bool Alive(EntityUid uid)
    {
        return TryComp<MobStateComponent>(uid, out var state) && state.CurrentState != MobState.Dead;
    }

    private int Remaining(EntityUid uid)
    {
        var max = 10;
        if (TryComp<DndHitPointsComponent>(uid, out var hitPoints))
            max = hitPoints.Max;

        return Math.Max(0, max - (int) _damageable.GetTotalDamage(uid).Float());
    }

    private int Roll(int dice, int sides)
    {
        if (dice < 1 || sides < 1)
            return 0;

        var total = 0;
        for (var i = 0; i < dice; i++)
            total += _random.Next(1, sides + 1);

        return total;
    }

    private void Expire<T>(TimeSpan now, Func<T, TimeSpan> until, Action<EntityUid>? after = null)
        where T : IComponent
    {
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (now < until(component))
                continue;

            RemComp<T>(uid);
            after?.Invoke(uid);
        }
    }
}
