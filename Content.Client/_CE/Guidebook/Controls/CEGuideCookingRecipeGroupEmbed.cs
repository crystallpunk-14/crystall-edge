using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Richtext;
using Content.Shared._CE.Cooking.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Guidebook.Controls;

/// <summary>
/// Control for listing CE cooking recipes by food type in a guidebook.
/// </summary>
[UsedImplicitly]
public sealed class CEGuideCookingRecipeGroupEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ISawmill _sawmill;

    public CEGuideCookingRecipeGroupEmbed()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.InjectDependencies(this);

        _sawmill = _logManager.GetSawmill("guidebook.ce_cooking_group");
        MouseFilter = MouseFilterMode.Stop;
    }

    public CEGuideCookingRecipeGroupEmbed(string foodType, string? plateTexture = null) : this()
    {
        CreateEntries(foodType, plateTexture);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!args.TryGetValue("FoodType", out var foodType))
        {
            _sawmill.Error("CE cooking recipe group embed tag is missing FoodType argument");
            return false;
        }

        args.TryGetValue("PlateTexture", out var plateTexture);

        CreateEntries(foodType, plateTexture);
        control = this;
        return true;
    }

    private void CreateEntries(string foodType, string? plateTexture = null)
    {
        var prototypes = _prototype.EnumeratePrototypes<CECookingRecipePrototype>()
            .Where(p => p.FoodType.Id.Equals(foodType))
            .OrderBy(GetRecipeComplexity)
            .ThenBy(p => Loc.GetString(p.FoodData.Name ?? "ce-guidebook-cooking-unknown-food-name"));

        foreach (var recipe in prototypes)
        {
            var embed = new CEGuideCookingRecipeEmbed(recipe);
            if (plateTexture != null)
                embed.SetPlateTexture(plateTexture);
            AddChild(embed);
        }
    }

    private static float GetRecipeComplexity(CECookingRecipePrototype recipe)
    {
        return recipe.Requirements.Sum(r => r.GetComplexity());
    }
}
