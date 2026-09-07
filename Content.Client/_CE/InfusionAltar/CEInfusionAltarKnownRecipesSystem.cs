using Content.Shared._CE.InfusionAltar;
using Robust.Shared.Analyzers;

namespace Content.Client._CE.InfusionAltar;

/// <summary>
/// Client-side access point for "which infusion altar recipes does the local player currently know".
/// Holds no state of its own between requests - callers (e.g. the recipe guidebook page) should call
/// <see cref="RequestKnownRecipes"/> and listen to <see cref="OnRecipesUpdated"/> for the answer.
/// </summary>
public sealed partial class CEInfusionAltarKnownRecipesSystem : EntitySystem
{
    public event Action<List<CEInfusionAltarKnownRecipeInfo>>? OnRecipesUpdated;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void RequestKnownRecipes()
    {
        RaiseNetworkEvent(new CERequestInfusionAltarKnownRecipesEvent());
    }

    [SubscribeNetworkEvent]
    private void OnUpdate(CEUpdateInfusionAltarKnownRecipesEvent ev)
    {
        OnRecipesUpdated?.Invoke(ev.Recipes);
    }
}
