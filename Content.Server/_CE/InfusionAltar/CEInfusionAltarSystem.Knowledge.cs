using Content.Shared._CE.InfusionAltar;
using Content.Shared._CE.InfusionAltar.Prototypes;

namespace Content.Server._CE.InfusionAltar;

public sealed partial class CEInfusionAltarSystem
{
    private void InitKnowledge()
    {
        SubscribeNetworkEvent<CERequestInfusionAltarKnownRecipesEvent>(OnRequestKnownRecipes);
    }

    private void OnRequestKnownRecipes(CERequestInfusionAltarKnownRecipesEvent ev, EntitySessionEventArgs args)
    {
        var recipes = new List<CEInfusionAltarKnownRecipeInfo>();

        if (TryGetSingleton(out var singleton))
        {
            var player = args.SenderSession.AttachedEntity;

            foreach (var recipe in _proto.EnumeratePrototypes<CEInfusionAltarRecipePrototype>())
            {
                if (!singleton.Recipes.TryGetValue(recipe.ID, out var cache))
                    continue;

                // Recipes with no knowledge requirement are shown to everyone unconditionally,
                // checked before anything player-specific (and even without an attached player
                // entity at all).
                if (recipe.RequiredKnowledge is { } required && (player is not { } p || !_knowledge.Knows(p, required)))
                    continue;

                // Cache.PedestalRequirements holds the exact same CEResourceRequirement instances as the
                // recipe's PedestalRequirementPool (RollPedestalRequirements never clones them), so
                // reference equality reliably recovers each rolled entry's pool index.
                var indices = new List<int>(cache.PedestalRequirements.Count);
                foreach (var requirement in cache.PedestalRequirements)
                {
                    var index = recipe.PedestalRequirementPool.FindIndex(entry => ReferenceEquals(entry.Requirement, requirement));
                    if (index >= 0)
                        indices.Add(index);
                }

                recipes.Add(new CEInfusionAltarKnownRecipeInfo(recipe.ID, cache.Essences, indices));
            }
        }

        RaiseNetworkEvent(new CEUpdateInfusionAltarKnownRecipesEvent(recipes), args.SenderSession);
    }
}
