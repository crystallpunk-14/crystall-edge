/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ZLevels.Damage;

public abstract partial class CEZLevelDamageSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public float BaseFallingDamage { get; private set; }
    public float BaseFallingStunTime { get; private set; }

    private static readonly ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<DamageableComponent, CEZLevelHitEvent>(OnFallDamage);

        _config.OnValueChanged(CCVars.CEBaseFallingDamage, i => BaseFallingDamage = i, true);
        _config.OnValueChanged(CCVars.CEBaseFallingStunTime, i => BaseFallingStunTime = i, true);
    }

    private void OnFallDamage(Entity<DamageableComponent> ent, ref CEZLevelHitEvent args)
    {
        var damageModifier = 1f;
        var stunModifier = 1f;

        var damageToOtherEv = new CEZFallingOnTargetDamageCalculateEvent();
        RaiseLocalEvent(ent, damageToOtherEv);
        var otherDamage = damageToOtherEv.DamageMultiplier * BaseFallingDamage * args.ImpactPower;
        var otherStun = damageToOtherEv.StunMultiplier * BaseFallingStunTime * args.ImpactPower;

        //Edit self damage
        var damageToSelfEv = new CEZFallingDamageCalculateEvent(ent);
        RaiseLocalEvent(ent, damageToSelfEv);
        damageModifier *= damageToSelfEv.DamageMultiplier;
        stunModifier *= damageToSelfEv.StunMultiplier;

        var entitiesAround = _lookup.GetEntitiesInRange(ent, 0.25f, LookupFlags.Uncontained);
        entitiesAround.Remove(ent); //Don't count self

        //Process entities we fell into
        var imFallOnEv = new CEZImFallOnEvent(entitiesAround, args.ImpactPower);
        RaiseLocalEvent(ent, imFallOnEv);

        foreach (var victim in entitiesAround)
        {
            //Other entities edit our damage
            var editDamageToSelfEv = new CEZFallingDamageCalculateEvent(ent);
            RaiseLocalEvent(victim, editDamageToSelfEv);
            damageModifier *= editDamageToSelfEv.DamageMultiplier;
            stunModifier *= editDamageToSelfEv.StunMultiplier;

            var fellOnMeEv = new CEZFellOnMeEvent(ent, args.ImpactPower);
            RaiseLocalEvent(victim, fellOnMeEv);

            //Affect other entities
            if (otherStun > 0)
                _stun.TryKnockdown(victim, TimeSpan.FromSeconds(otherStun));
            if (otherDamage > 0)
                _damage.TryChangeDamage(victim, new DamageSpecifier(_proto.Index(BluntDamageType), otherDamage));
        }

        var damageAmount = args.ImpactPower * BaseFallingDamage * damageModifier;
        if (damageAmount > 0)
            _damage.TryChangeDamage(ent.Owner, new DamageSpecifier(_proto.Index(BluntDamageType), damageAmount));

        var knockdownTime = MathF.Min(args.ImpactPower * BaseFallingStunTime * stunModifier, 5f);
        if (knockdownTime > 0)
            _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(knockdownTime));
    }
}

/// <summary>
/// This event is triggered both on the entity that fell and on all entities that it fell on.
/// Together, they calculate the damage and the duration that should be applied to the fallen entity.
/// </summary>
public sealed class CEZFallingDamageCalculateEvent(EntityUid fallen) : EntityEventArgs
{
    public EntityUid Fallen = fallen;

    public float DamageMultiplier = 1;
    public float StunMultiplier = 1;
}

/// <summary>
/// Called on a falling entity to calculate how much damage it should inflict on everything it falls on.
/// </summary>
public sealed class CEZFallingOnTargetDamageCalculateEvent() : EntityEventArgs
{
    public float DamageMultiplier = 1;
    public float StunMultiplier = 1;
}

/// <summary>
/// Event raised on a falling entity to inform it about the entities it is landing on and the impact speed.
/// </summary>
public sealed class CEZImFallOnEvent(HashSet<EntityUid> targets, float speed) : EntityEventArgs
{
    public HashSet<EntityUid> Targets = targets;
    public float Speed = speed;
}

/// <summary>
/// Event raised on an entity that is being fallen on to inform it about the falling entity and the impact speed.
/// </summary>
public sealed class CEZFellOnMeEvent(EntityUid fallen, float speed) : EntityEventArgs
{
    public EntityUid Fallen = fallen;
    public float Speed = speed;
}
