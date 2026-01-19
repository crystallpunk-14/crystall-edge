/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Requirements;

public sealed partial class AlwaysMet : CECookingCraftRequirement
{
    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        List<ProtoId<CEFoodTagPrototype>> placedFoodTags,
        Solution? solution = null)
    {
        return true;
    }

    public override float GetComplexity()
    {
        return 0;
    }

    public override string GetGuidebookDescription(IPrototypeManager protoManager)
    {
        return Loc.GetString("ce-guidebook-cooking-requirement-any");
    }
}
