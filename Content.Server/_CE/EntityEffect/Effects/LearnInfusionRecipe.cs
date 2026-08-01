using Content.Server._CE.InfusionAltar;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;

namespace Content.Server._CE.EntityEffect.Effects;

/// <summary>
/// Server-side handler for the <see cref="LearnInfusionRecipe"/> CE entity effect.
/// Adds the specified recipes to the target entity's infusion altar recipe knowledge.
/// </summary>
public sealed partial class CELearnInfusionRecipeEffectSystem : CEEntityEffectSystem<LearnInfusionRecipe>
{
    [Dependency] private CEInfusionAltarSystem _infusionAltar = default!;

    protected override void Effect(ref CEEntityEffectEvent<LearnInfusionRecipe> args)
    {
        var target = ResolveEffectEntity(args.Args, args.Effect.EffectTarget);
        if (target is not { } entity)
            return;

        foreach (var recipe in args.Effect.Recipes)
        {
            _infusionAltar.TryAddRecipe(entity, recipe);
        }
    }
}
