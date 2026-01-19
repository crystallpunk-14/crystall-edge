/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Requirements;

public sealed partial class TagRequired : CECookingCraftRequirement
{
    /// <summary>
    /// Any of this tags accepted
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<TagPrototype>> Tags = default!;

    [DataField]
    public bool AllowOtherTags = true;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        List<ProtoId<TagPrototype>> placedTags,
        Solution? solution = null)
    {
        foreach (var placedTag in placedTags)
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
        var tags = string.Join(", ", Tags.Select(t => t.Id));
        return Loc.GetString(
            "ce-guidebook-cooking-requirement-tag-required",
            ("tags", tags));
    }
}
