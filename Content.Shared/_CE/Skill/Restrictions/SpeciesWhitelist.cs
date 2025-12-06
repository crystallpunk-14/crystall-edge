using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill.Restrictions;

public sealed partial class SpeciesWhitelist : CESkillRestriction
{
    public override bool HideFromUI => true;

    [DataField(required: true)]
    public ProtoId<SpeciesPrototype> Species = new();

    public override bool Check(IEntityManager entManager, EntityUid target)
    {
        if (!entManager.TryGetComponent<HumanoidAppearanceComponent>(target, out var appearance))
            return false;

        return appearance.Species == Species;
    }

    public override string GetDescription(IEntityManager entManager, IPrototypeManager protoManager)
    {
        var species = protoManager.Index(Species);

        return Loc.GetString("ce-skill-req-species", ("name", Loc.GetString(species.Name)));
    }
}
