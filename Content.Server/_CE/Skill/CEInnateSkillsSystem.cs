using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;
using Robust.Shared.Analyzers;

namespace Content.Server._CE.Skill;

public sealed partial class CEInnateSkillsSystem : EntitySystem
{
    [Dependency] private CESharedSkillSystem _skill = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEInnateSkillsComponent> ent, ref MapInitEvent args)
    {
        var descriptor = new CESkillDescriptor
        {
            Name = ent.Comp.DescriptorName,
            Color = ent.Comp.DescriptorColor,
            Tooltip = ent.Comp.DescriptorTooltip,
        };

        foreach (var skill in ent.Comp.Skills)
            _skill.TryAddSkill(ent.Owner, skill, force: true, descriptor: descriptor);
    }
}
