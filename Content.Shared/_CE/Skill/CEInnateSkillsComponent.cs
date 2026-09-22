using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill;

/// <summary>
/// Grants a fixed set of skills the moment this entity is initialized (e.g. a species' or ghost
/// role's inborn abilities). All skills share one <see cref="CESkillDescriptor"/>-equivalent badge
/// (name/color/tooltip) naming this component as their source in the character menu.
/// </summary>
[RegisterComponent]
public sealed partial class CEInnateSkillsComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<CESkillPrototype>> Skills = new();

    /// <summary>
    /// Source badge name shown on each granted skill's card in the character menu.
    /// </summary>
    [DataField(required: true)]
    public LocId DescriptorName;

    [DataField]
    public Color DescriptorColor = Color.White;

    [DataField]
    public LocId? DescriptorTooltip;
}
