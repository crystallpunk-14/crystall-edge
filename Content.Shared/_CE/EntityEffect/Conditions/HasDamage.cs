using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity has damage of any listed type or group, or any damage at all if nothing is listed.
/// </summary>
public sealed partial class HasDamage : CEEntityConditionBase<HasDamage>
{
    [DataField]
    public List<ProtoId<DamageTypePrototype>> Types = new();

    [DataField]
    public List<ProtoId<DamageGroupPrototype>> Groups = new();
}

public sealed partial class CEHasDamageConditionSystem : CEEntityConditionSystem<HasDamage>
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    protected override void Condition(ref CEEntityConditionEvent<HasDamage> args)
    {
        var damage = _damageable.GetAllDamage(args.Entity).DamageDict;
        var condition = args.Condition;

        if (condition.Types.Count == 0 && condition.Groups.Count == 0)
        {
            foreach (var amount in damage.Values)
            {
                if (amount <= 0)
                    continue;

                args.Result = true;
                return;
            }

            return;
        }

        foreach (var type in condition.Types)
        {
            if (!damage.TryGetValue(type, out var amount) || amount <= 0)
                continue;

            args.Result = true;
            return;
        }

        foreach (var groupId in condition.Groups)
        {
            if (!_proto.Resolve(groupId, out var group))
                continue;

            foreach (var type in group.DamageTypes)
            {
                if (!damage.TryGetValue(type, out var amount) || amount <= 0)
                    continue;

                args.Result = true;
                return;
            }
        }
    }
}
