/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Prototypes;

[Prototype("foodType")]
public sealed partial class CEFoodTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Dish entity drawn under the food layers in resource icons.
    /// </summary>
    [DataField]
    public EntProtoId? IconHolder;

    /// <summary>
    /// Recipe that replaces food of this type when it gets overcooked.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CECookingRecipePrototype> BurntRecipe;
}
