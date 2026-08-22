using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Heals damage groups on the resolved target entity via <see cref="DamageableSystem.HealDistributed"/>.
/// The amount healed per type within a group is weighted by how much damage of that type is present.
/// </summary>
public sealed partial class Heal : CEEntityEffectBase<Heal>
{
    /// <summary>
    /// How much to heal from each damage group. Amounts are written as positive numbers.
    /// </summary>
    [DataField("groups", required: true)]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> Groups = new();

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
    }
}
