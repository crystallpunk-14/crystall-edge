using System.Diagnostics.CodeAnalysis;
using Content.Client._CE.InfusionAltar;
using Content.Client.Guidebook.Richtext;
using Content.Shared._CE.InfusionAltar;
using Content.Shared._CE.InfusionAltar.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Guidebook.Controls;

/// <summary>
/// Lists the infusion altar recipes currently known to the local player (round-start recipes plus
/// anything they've been taught), each as a <see cref="CEGuideInfusionAltarRecipeEmbed"/> card. Recipe
/// knowledge is per-player hidden server state, so this asks the server fresh via
/// <see cref="CEInfusionAltarKnowledgeSystem"/> every time the page is opened instead of reading any
/// locally cached data.
/// </summary>
[UsedImplicitly]
public sealed partial class CEGuideInfusionAltarRecipesEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IEntityManager _entity = default!;

    private readonly CEInfusionAltarKnowledgeSystem _knowledge;
    private readonly Label _emptyLabel;

    public CEGuideInfusionAltarRecipesEmbed()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Stop;

        _knowledge = _entity.System<CEInfusionAltarKnowledgeSystem>();
        _emptyLabel = new Label { Text = Loc.GetString("ce-guidebook-infusion-altar-none-known") };
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        _knowledge.OnRecipesUpdated += OnRecipesUpdated;
        _knowledge.RequestKnownRecipes();

        AddChild(_emptyLabel);

        control = this;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _knowledge.OnRecipesUpdated -= OnRecipesUpdated;
    }

    private void OnRecipesUpdated(List<CEInfusionAltarKnownRecipeInfo> recipes)
    {
        RemoveAllChildren();

        if (recipes.Count == 0)
        {
            AddChild(_emptyLabel);
            return;
        }

        foreach (var info in recipes)
        {
            if (!_prototype.TryIndex(info.Recipe, out CEInfusionAltarRecipePrototype? recipe))
                continue;

            AddChild(new CEGuideInfusionAltarRecipeEmbed(recipe, info));
        }
    }
}
