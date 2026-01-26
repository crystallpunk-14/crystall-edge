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
        // Complexity is inversely proportional to the number of alternative tags
        // 1 tag = high complexity (2.0), 2 tags = 1.0, 3 tags = 0.67, etc.
        // This represents that having more options makes it easier to fulfill the requirement
        return Tags.Count > 0 ? 2.0f / Tags.Count : 2.0f;
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
