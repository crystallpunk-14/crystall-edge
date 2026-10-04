using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the target entity's species matches. Combine with <see cref="CEEntityCondition.Inverted"/>
/// to express a species blacklist instead of a whitelist.
/// </summary>
public sealed partial class IsSpecies : CEEntityConditionBase<IsSpecies>
{
    [DataField(required: true)]
    public ProtoId<SpeciesPrototype> Species;

    public override string GetDescription(IEntityManager entityManager, IPrototypeManager prototype)
    {
        var species = prototype.Index(Species);
        var key = Inverted ? "ce-skill-req-notspecies" : "ce-skill-req-species";
        return Loc.GetString(key, ("name", Loc.GetString(species.Name)));
    }
}

public sealed partial class CEIsSpeciesConditionSystem : CEEntityConditionSystem<IsSpecies>
{
    protected override void Condition(ref CEEntityConditionEvent<IsSpecies> args)
    {
        args.Result = TryComp<HumanoidProfileComponent>(args.Entity, out var appearance)
            && appearance.Species == args.Condition.Species;
    }
}
