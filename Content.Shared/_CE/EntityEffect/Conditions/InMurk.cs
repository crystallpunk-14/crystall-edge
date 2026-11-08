using Content.Shared._CE.Murk;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity or targeted position is in the murk. Inverted, it requires the safe zone
/// of the Lucson Sphere or a murk shield.
/// </summary>
public sealed partial class InMurk : CEEntityConditionBase<InMurk>
{
    protected override bool SupportsPosition => true;

    protected override bool CheckPosition(EntityCoordinates position, CEEntityEffectArgs args)
    {
        return args.EntityManager.System<CESharedMurkSystem>().InMurk(position);
    }

    public override string GetDescription(IEntityManager entityManager, IPrototypeManager prototype) =>
        Loc.GetString(Inverted ? "ce-in-murk-forbidden" : "ce-in-murk-required");
}

public sealed partial class CEInMurkConditionSystem : CEEntityConditionSystem<InMurk>
{
    [Dependency] private CESharedMurkSystem _murk = default!;

    protected override void Condition(ref CEEntityConditionEvent<InMurk> args)
    {
        args.Result = _murk.InMurk(args.Entity);
    }
}
