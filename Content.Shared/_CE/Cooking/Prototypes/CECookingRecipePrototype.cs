/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Prototypes;

[Prototype("CECookingRecipe")]
public sealed class CECookingRecipePrototype : IPrototype
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

    [DataField]
    public TimeSpan CookingTime = TimeSpan.FromSeconds(20f);
}
