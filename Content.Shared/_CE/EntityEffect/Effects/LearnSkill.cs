using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Teaches the target entity a set of skills. Handled by <c>CELearnSkillEffectSystem</c>.
/// </summary>
public sealed partial class LearnSkill : CEEntityEffectBase<LearnSkill>
{
    public LearnSkill()
    {
        EffectTarget = CEEffectTarget.Target;
    }

    /// <summary>
    /// Skills to teach to the target.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CESkillPrototype>> Skills = new();

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var skillSystem = entSys.GetEntitySystem<CESharedSkillSystem>();

        var lines = new List<string>();
        foreach (var skillId in Skills)
        {
            if (!prototype.HasIndex(skillId))
                continue;

            var header = Loc.GetString("ce-entity-effect-guidebook-learn-skill", ("name", skillSystem.GetSkillName(skillId)));
            var effects = skillSystem.GetSkillEffectDescription(skillId);

            lines.Add(effects.Length > 0 ? $"{header}\n{effects}" : header);
        }

        return lines.Count > 0 ? string.Join("\n", lines) : base.EntityEffectGuidebookText(prototype, entSys);
    }
}

public sealed partial class CELearnSkillEffectSystem : CEEntityEffectSystem<LearnSkill>
{
    [Dependency] private CESharedSkillSystem _skill = default!;

    protected override void Effect(ref CEEntityEffectEvent<LearnSkill> args)
    {
        var target = ResolveEffectEntity(args.Args, args.Effect.EffectTarget);
        if (target is not { } entity)
            return;

        foreach (var skill in args.Effect.Skills)
        {
            _skill.TryAddSkill(entity, skill);
        }
    }
}
