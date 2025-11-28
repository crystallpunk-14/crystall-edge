using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill.Effects;

public sealed partial class AddComponents : CESkillEffect
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    public override void AddSkill(IEntityManager entManager, EntityUid target)
    {
        entManager.AddComponents(target, Components);
    }

    public override void RemoveSkill(IEntityManager entManager, EntityUid target)
    {
        entManager.RemoveComponents(target, Components);
    }

    public override string? GetName(IEntityManager entManager, IPrototypeManager protoManager)
    {
        return null;
    }

    public override string? GetDescription(IEntityManager entManager, IPrototypeManager protoManager, ProtoId<CESkillPrototype> skill)
    {
        return null;
    }
}
