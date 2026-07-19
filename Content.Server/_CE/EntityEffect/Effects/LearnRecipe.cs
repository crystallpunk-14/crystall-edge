using Content.Server._CE.Workbench;
using Content.Server.Popups;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;

namespace Content.Server._CE.EntityEffect.Effects;

/// <summary>
/// Server-side handler for the <see cref="LearnRecipe"/> CE entity effect.
/// Adds the specified recipes to the target entity's recipe knowledge.
/// </summary>
public sealed partial class CELearnRecipeEffectSystem : CEEntityEffectSystem<LearnRecipe>
{
    [Dependency] private CERecipeKnowledgeSystem _recipeKnowledge = default!;
    [Dependency] private PopupSystem _popup = default!;

    protected override void Effect(ref CEEntityEffectEvent<LearnRecipe> args)
    {
        var target = ResolveEffectEntity(args.Args, args.Effect.EffectTarget);
        if (target is not { } entity)
            return;

        var knowledge = EnsureComp<CERecipeKnowledgeComponent>(entity);

        var newlyLearned = 0;
        foreach (var recipe in args.Effect.Recipes)
        {
            if (_recipeKnowledge.TryAddRecipe(entity, recipe, knowledge))
                newlyLearned++;
        }

        if (newlyLearned > 0)
            _popup.PopupEntity(Loc.GetString("ce-recipe-learned"), entity, entity);
        else
            _popup.PopupEntity(Loc.GetString("ce-recipe-already-known"), entity, entity);
    }
}
