using Content.Shared._CE.SpellMastery;
using Content.Shared._CE.SpellMastery.Prototypes;

namespace Content.Server._CE.SpellMastery;

// CrystallEdge: rough copy of Content.Server._CE.InfusionAltar.CEInfusionAltarSystem.Skill, not yet
// cleaned up/unified.

public sealed partial class CESpellMasterySystem
{
    private void InitSkills()
    {
        SubscribeNetworkEvent<CERequestSpellMasteryKnownRecipesEvent>(OnRequestKnownRecipes);
    }

    private void OnRequestKnownRecipes(CERequestSpellMasteryKnownRecipesEvent ev, EntitySessionEventArgs args)
    {
        var recipes = new List<CESpellMasteryKnownRecipeInfo>();

        if (TryGetSingleton(out var singleton))
        {
            var player = args.SenderSession.AttachedEntity;

            foreach (var recipe in _proto.EnumeratePrototypes<CESpellMasteryRitualRecipePrototype>())
            {
                if (!singleton.Recipes.TryGetValue(recipe.ID, out var cache))
                    continue;

                // Recipes with no skill requirement are shown to everyone unconditionally,
                // checked before anything player-specific (and even without an attached player
                // entity at all).
                if (recipe.RequiredSkill is { } required && (player is not { } p || !_skill.HaveSkill(p, required)))
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

                recipes.Add(new CESpellMasteryKnownRecipeInfo(recipe.ID, cache.Essences, indices));
            }
        }

        RaiseNetworkEvent(new CEUpdateSpellMasteryKnownRecipesEvent(recipes), args.SenderSession);
    }
}
