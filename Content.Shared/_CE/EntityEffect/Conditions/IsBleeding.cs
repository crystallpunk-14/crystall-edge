using Content.Shared.Body.Components;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity is bleeding.
/// </summary>
public sealed partial class IsBleeding : CEEntityConditionBase<IsBleeding>;

public sealed partial class CEIsBleedingConditionSystem : CEEntityConditionSystem<IsBleeding>
{
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<IsBleeding> args)
    {
        args.Result = _bloodstreamQuery.TryComp(args.Entity, out var bloodstream) && bloodstream.BleedAmount > 0;
    }
}
