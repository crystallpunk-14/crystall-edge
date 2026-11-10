using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity can be injured and its damage container is one of <see cref="Containers"/>.
/// </summary>
public sealed partial class DamageContainer : CEEntityConditionBase<DamageContainer>
{
    [DataField(required: true)]
    public List<ProtoId<DamageContainerPrototype>> Containers = new();
}

public sealed partial class CEDamageContainerConditionSystem : CEEntityConditionSystem<DamageContainer>
{
    [Dependency] private EntityQuery<InjurableComponent> _injurableQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<DamageContainer> args)
    {
        args.Result = _injurableQuery.TryComp(args.Entity, out var injurable)
            && injurable.DamageContainer is { } container
            && args.Condition.Containers.Contains(container);
    }
}
