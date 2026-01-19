/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Requirements;

public sealed partial class TagBlocked : CECookingCraftRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<TagPrototype>> Tags = default!;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        List<ProtoId<TagPrototype>> placedTags,
        Solution? solution = null)
    {
        foreach (var placedTag in placedTags)
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
        var tags = string.Join(", ", Tags.Select(t => t.Id));
        return Loc.GetString(
            "ce-guidebook-cooking-requirement-tag-blocked",
            ("tags", tags));
    }
}
