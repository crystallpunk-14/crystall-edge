/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking;

/// <summary>
/// An abstract condition that is a key element of the system. The more complex the conditions for a recipe,
/// the more difficult it is to "get" that recipe by collecting ingredients at random.
/// The system automatically calculates the complexity of a recipe using GetComplexity() for each condition.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CECookingCraftRequirement
{
    public abstract bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        List<ProtoId<CEFoodTagPrototype>> placedFoodTags,
        Solution? solution = null);

    public abstract float GetComplexity();

    /// <summary>
    /// Returns a formatted string describing this requirement for guidebook display.
    /// </summary>
    public abstract string GetGuidebookDescription(IPrototypeManager protoManager);
}
