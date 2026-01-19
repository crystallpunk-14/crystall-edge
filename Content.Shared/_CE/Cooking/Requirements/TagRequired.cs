/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared.Chemistry.Components;
using Content.Shared._CE.Cooking.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Requirements;

public sealed partial class TagRequired : CECookingCraftRequirement
{
    /// <summary>
    /// Any of this tags accepted
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CEFoodTagPrototype>> Tags = default!;

    [DataField]
    public bool AllowOtherTags = true;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        List<ProtoId<CEFoodTagPrototype>> placedFoodTags,
        Solution? solution = null)
    {
        foreach (var placedTag in placedFoodTags)
        {
            if (Tags.Contains(placedTag))
                return true;
        }

        return false;
    }

    public override float GetComplexity()
    {
        return AllowOtherTags ? 5 : 1;
    }

    public override string GetGuidebookDescription(IPrototypeManager protoManager)
    {
        var names = new List<string>();
        foreach (var tag in Tags)
        {
            if (protoManager.TryIndex(tag, out var foodTag))
                names.Add(Loc.GetString(foodTag.Name));
            else
                names.Add(tag.Id);
        }

        var separator = Loc.GetString("ce-guidebook-cooking-or-separator");
        var tags = string.Join($" {separator} ", names);
        return Loc.GetString(
            "ce-guidebook-cooking-requirement-tag-required",
            ("tags", tags));
    }
}
