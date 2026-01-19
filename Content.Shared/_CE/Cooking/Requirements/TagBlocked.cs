/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared.Chemistry.Components;
using Content.Shared._CE.Cooking.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Requirements;

public sealed partial class TagBlocked : CECookingCraftRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<CEFoodTagPrototype>> Tags = default!;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        List<ProtoId<CEFoodTagPrototype>> placedFoodTags,
        Solution? solution = null)
    {
        foreach (var placedTag in placedFoodTags)
        {
            if (Tags.Contains(placedTag))
                return false;
        }

        return true;
    }

    public override float GetComplexity()
    {
        return Tags.Count * -1;
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

        var tags = string.Join(", ", names);
        return Loc.GetString(
            "ce-guidebook-cooking-requirement-tag-blocked",
            ("tags", tags));
    }
}
