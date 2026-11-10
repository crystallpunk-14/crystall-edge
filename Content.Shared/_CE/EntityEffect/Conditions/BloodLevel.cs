using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity's blood level, as a fraction of normal volume, is at least <see cref="Min"/> and below <see cref="Max"/>.
/// </summary>
public sealed partial class BloodLevel : CEEntityConditionBase<BloodLevel>
{
    [DataField]
    public float Min;

    [DataField]
    public float Max = 1f;
}

public sealed partial class CEBloodLevelConditionSystem : CEEntityConditionSystem<BloodLevel>
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<BloodLevel> args)
    {
        if (!_bloodstreamQuery.TryComp(args.Entity, out var bloodstream))
            return;

        var level = _bloodstream.GetBloodLevel((args.Entity, bloodstream));
        args.Result = level >= args.Condition.Min && level < args.Condition.Max;
    }
}
