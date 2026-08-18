using Content.Shared._CE.InfusionAltar;

namespace Content.Client._CE.InfusionAltar;

/// <summary>
/// Client-side access point for "which infusion altar recipes does the local player currently know".
/// Holds no state of its own between requests - callers (e.g. the recipe guidebook page) should call
/// <see cref="RequestKnownRecipes"/> and listen to <see cref="OnRecipesUpdated"/> for the answer.
/// </summary>
public sealed partial class CEInfusionAltarKnowledgeSystem : EntitySystem
{
    public event Action<List<CEInfusionAltarKnownRecipeInfo>>? OnRecipesUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CEUpdateInfusionAltarKnownRecipesEvent>(OnUpdate);
    }

    public void RequestKnownRecipes()
    {
        RaiseNetworkEvent(new CERequestInfusionAltarKnownRecipesEvent());
    }

    private void OnUpdate(CEUpdateInfusionAltarKnownRecipesEvent ev)
    {
        OnRecipesUpdated?.Invoke(ev.Recipes);
    }
}
