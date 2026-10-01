using Content.Shared._CE.Murk.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity's murk dissolution level is at least <see cref="Min"/>.
/// </summary>
public sealed partial class MurkDissolved : CEEntityConditionBase<MurkDissolved>
{
    [DataField]
    public float Min = 1f;

    public override string GetDescription(IEntityManager entityManager, IPrototypeManager prototype) =>
        Loc.GetString("ce-murk-dissolved-required");
}

public sealed partial class CEMurkDissolvedConditionSystem : CEEntityConditionSystem<MurkDissolved>
{
    [Dependency] private EntityQuery<CEMurkDissolvingStatusComponent> _statusQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<MurkDissolved> args)
    {
        args.Result = _statusQuery.TryComp(args.Entity, out var status) && status.Dissolved >= args.Condition.Min;
    }
}
