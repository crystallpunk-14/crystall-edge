/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using YamlDotNet.Serialization.Schemas;

namespace Content.Shared._CE.Cooking.Requirements;

public sealed partial class ReagentRequired : CECookingCraftRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<ReagentPrototype>> Reagents = default!;

    [DataField]
    public FixedPoint2 Amount = 10f;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        List<ProtoId<CEFoodTagPrototype>> placedFoodTags,
        Solution? solution = null)
    {
        if (solution is null)
            return false;

        var passed = false;
        foreach (var (reagent, quantity) in solution.Contents)
        {
            if (!Reagents.Contains(reagent.Prototype))
                continue;

            if (quantity < Amount)
                continue;

            passed = true;
            break;
        }

        return passed;
    }

    public override float GetComplexity()
    {
        return 1;
    }

    public override string GetGuidebookDescription(IPrototypeManager protoManager)
    {
        var names = new List<string>();
        foreach (var reagentId in Reagents)
        {
            if (protoManager.TryIndex(reagentId, out var reagent))
                names.Add(reagent.LocalizedName);
            else
                names.Add(reagentId.Id);
        }

        var reagents = string.Join(", ", names);
        return Loc.GetString(
            "ce-guidebook-cooking-requirement-reagent-required",
            ("reagents", reagents),
            ("amount", Amount.ToString()));
    }
}
