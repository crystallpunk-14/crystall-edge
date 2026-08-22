using Content.Shared._CE.SpellMastery;

namespace Content.Client._CE.SpellMastery;

// CrystallEdge: rough copy of Content.Client._CE.InfusionAltar.CEInfusionAltarKnownRecipesSystem, not
// yet cleaned up/unified.

/// <summary>
/// Client-side access point for "which spell mastery recipes does the local player currently know".
/// Holds no state of its own between requests - callers (e.g. the recipe guidebook page) should call
/// <see cref="RequestKnownRecipes"/> and listen to <see cref="OnRecipesUpdated"/> for the answer.
/// </summary>
public sealed partial class CESpellMasteryKnownRecipesSystem : EntitySystem
{
    public event Action<List<CESpellMasteryKnownRecipeInfo>>? OnRecipesUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CEUpdateSpellMasteryKnownRecipesEvent>(OnUpdate);
    }

    public void RequestKnownRecipes()
    {
        RaiseNetworkEvent(new CERequestSpellMasteryKnownRecipesEvent());
    }

    private void OnUpdate(CEUpdateSpellMasteryKnownRecipesEvent ev)
    {
        OnRecipesUpdated?.Invoke(ev.Recipes);
    }
}
