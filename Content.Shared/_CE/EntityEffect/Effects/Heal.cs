using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Heals damage groups via <see cref="DamageableSystem.HealDistributed"/> and exact damage types on the resolved target.
/// </summary>
public sealed partial class Heal : CEEntityEffectBase<Heal>
{
    /// <summary>
    /// How much to heal from each damage group, weighted by how much damage of each type is present. Positive numbers.
    /// </summary>
    [DataField("groups")]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> Groups = new();

    /// <summary>
    /// How much to heal of each damage type, ignoring resistances. Positive numbers.
    /// </summary>
    [DataField("types")]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> Types = new();

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var parts = new List<string>();
        foreach (var (group, amount) in Groups)
        {
            if (amount == 0)
                continue;

            var groupName = prototype.TryIndex(group, out var groupProto) ? groupProto.LocalizedName : group.Id;
            parts.Add(Loc.GetString("ce-entity-effect-guidebook-heal-entry", ("amount", amount.Float()), ("group", groupName)));
        }

        foreach (var (type, amount) in Types)
        {
            if (amount == 0)
                continue;

            var typeName = prototype.TryIndex(type, out var typeProto) ? typeProto.LocalizedName : type.Id;
            parts.Add(Loc.GetString("ce-entity-effect-guidebook-heal-entry", ("amount", amount.Float()), ("group", typeName)));
        }

        if (parts.Count == 0)
            return base.EntityEffectGuidebookText(prototype, entSys);

        return Loc.GetString("ce-entity-effect-guidebook-heal", ("heals", string.Join(", ", parts)));
    }
}

public sealed partial class CEHealEffectSystem : CEEntityEffectSystem<Heal>
{
    [Dependency] private DamageableSystem _damageable = default!;

    protected override void Effect(ref CEEntityEffectEvent<Heal> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        foreach (var (group, amount) in args.Effect.Groups)
        {
            _damageable.HealDistributed(entity, -amount * args.Args.Power, group, args.Args.Source);
        }

        if (args.Effect.Types.Count == 0)
            return;

        var heal = new DamageSpecifier();
        foreach (var (type, amount) in args.Effect.Types)
        {
            heal.DamageDict[type] = -amount * args.Args.Power;
        }

        _damageable.TryChangeDamage(entity, heal, ignoreResistances: true, interruptsDoAfters: false, origin: args.Args.Source);
    }
}
