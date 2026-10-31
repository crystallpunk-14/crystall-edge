using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Skill;

/// <summary>
/// Grants every humanoid the inborn skills of its species, badged with the species name.
/// </summary>
public sealed partial class CESpeciesSkillsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private CESharedSkillSystem _skill = default!;

    [SubscribeLocalEvent(after: [typeof(CESkillSystem)])]
    private void OnMapInit(Entity<HumanoidProfileComponent> ent, ref MapInitEvent args)
    {
        if (!_proto.Resolve(ent.Comp.Species, out var species) || species.Skills.Count == 0)
            return;

        var descriptor = new CESkillDescriptor
        {
            Name = species.Name,
            Color = species.SkillsBadgeColor,
        };

        foreach (var skill in species.Skills)
        {
            _skill.TryAddSkill(ent.Owner, skill, force: true, descriptor: descriptor);
        }
    }
}
