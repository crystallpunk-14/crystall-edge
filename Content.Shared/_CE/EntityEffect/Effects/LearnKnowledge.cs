using Content.Shared._CE.Knowledge;
using Content.Shared._CE.Knowledge.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Teaches the target entity a set of knowledge. Handled by <c>CELearnKnowledgeEffectSystem</c>.
/// </summary>
public sealed partial class LearnKnowledge : CEEntityEffectBase<LearnKnowledge>
{
    public LearnKnowledge()
    {
        EffectTarget = CEEffectTarget.Target;
    }

    /// <summary>
    /// Knowledge to teach to the target.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CEKnowledgePrototype>> Knowledge = new();

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var lines = new List<string>();
        foreach (var knowledgeId in Knowledge)
        {
            if (!prototype.TryIndex(knowledgeId, out var knowledge))
                continue;

            lines.Add(Loc.GetString("ce-entity-effect-guidebook-learn-knowledge", ("name", Loc.GetString(knowledge.Name))));
        }

        return lines.Count > 0 ? string.Join("\n", lines) : base.EntityEffectGuidebookText(prototype, entSys);
    }
}

public sealed partial class CELearnKnowledgeEffectSystem : CEEntityEffectSystem<LearnKnowledge>
{
    [Dependency] private CEKnowledgeSystem _knowledge = default!;

    protected override void Effect(ref CEEntityEffectEvent<LearnKnowledge> args)
    {
        var target = ResolveEffectEntity(args.Args, args.Effect.EffectTarget);
        if (target is not { } entity)
            return;

        foreach (var knowledge in args.Effect.Knowledge)
        {
            _knowledge.TryLearn(entity, knowledge);
        }
    }
}
