using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity is a mob in the given <see cref="State"/>.
/// </summary>
public sealed partial class IsMobState : CEEntityConditionBase<IsMobState>
{
    [DataField(required: true)]
    public MobState State;

    public override string GetDescription(IEntityManager entityManager, IPrototypeManager prototype)
    {
        var state = Loc.GetString($"ce-mob-state-{State.ToString().ToLowerInvariant()}");
        return Loc.GetString(Inverted ? "ce-mob-state-forbidden" : "ce-mob-state-required", ("state", state));
    }
}

public sealed partial class CEIsMobStateConditionSystem : CEEntityConditionSystem<IsMobState>
{
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<IsMobState> args)
    {
        args.Result = _mobStateQuery.TryComp(args.Entity, out var mobState)
            && mobState.CurrentState == args.Condition.State;
    }
}
