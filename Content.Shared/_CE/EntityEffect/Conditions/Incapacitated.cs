using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity is a mob in critical or dead state. Use with <see cref="CEEntityCondition.Inverted"/>
/// to require a conscious target.
/// </summary>
public sealed partial class Incapacitated : CEEntityConditionBase<Incapacitated>
{
}

public sealed partial class CEIncapacitatedConditionSystem : CEEntityConditionSystem<Incapacitated>
{
    [Dependency] private MobStateSystem _mobState = default!;

    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<Incapacitated> args)
    {
        args.Result = _mobStateQuery.TryComp(args.Entity, out var mobState)
            && _mobState.IsIncapacitated(args.Entity, mobState);
    }
}
