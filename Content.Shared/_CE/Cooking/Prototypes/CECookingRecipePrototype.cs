/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using Content.Shared._CE.Cooking.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Prototypes;

[Prototype("CECookingRecipe")]
public sealed partial class CECookingRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// List of conditions that must be met in the set of ingredients for a dish
    /// </summary>
    [DataField]
    public List<CECookingCraftRequirement> Requirements = new();

    /// <summary>
    /// Reagents cannot store all the necessary information about food, so along with the reagents for all the ingredients,
    /// in this block we add the appearance of the dish, descriptions, and so on.
    /// </summary>
    [DataField]
    public CEFoodData FoodData = new();

    [DataField(required: true)]
    public ProtoId<CEFoodTypePrototype> FoodType;

    /// <summary>
    /// Calculates the total complexity of this recipe by summing the complexity of all requirements.
    /// </summary>
    public float GetComplexity()
    {
        return Math.Max(0, Requirements.Sum(r => r.GetComplexity()));
    }
}
