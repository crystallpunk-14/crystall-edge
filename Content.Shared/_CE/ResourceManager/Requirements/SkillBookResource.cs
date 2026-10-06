using System.Numerics;
using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared._CE.Trade;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ResourceManager.Requirements;

/// <summary>
/// Requires books teaching a specific skill.
/// </summary>
public sealed partial class SkillBookResource : CEResourceRequirement
{
    private static readonly Vector2 BookOffset = new(-0.15f, 0.15f);
    private static readonly Vector2 SkillIconOffset = new(0.15f, -0.15f);

    [DataField(required: true)]
    public ProtoId<CESkillPrototype> Skill;

    [DataField]
    public int Count = 1;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        var count = 0;
        foreach (var ent in placedEntities)
        {
            if (IsMatchingBook(entManager, ent))
                count++;
        }

        return count >= Count;
    }

    public override void PostCraft(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        var requiredCount = Count;
        foreach (var ent in placedEntities)
        {
            if (requiredCount <= 0)
                return;

            if (!IsMatchingBook(entManager, ent))
                continue;

            entManager.DeleteEntity(ent);
            requiredCount--;
        }
    }

    public override double GetPrice(IEntityManager entManager, IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Skill, out var indexedSkill))
            return 0;

        var coverPrice = protoManager.TryIndex(indexedSkill.Book, out var indexedBook)
            ? entManager.System<CEEconomySystem>().GetEstimatedPrice(indexedBook)
            : 0;

        return (coverPrice + indexedSkill.Price) * Count;
    }

    public override string GetRequirementTitle(IPrototypeManager protoManager)
    {
        var skillSys = IoCManager.Resolve<IEntityManager>().System<CESharedSkillSystem>();
        return $"{skillSys.GetSkillName(Skill)} x{Count}";
    }

    public override string GetRequirementAmount()
    {
        return Count.ToString();
    }

    public override List<CEResourceIconLayer> GetRequirementIcon(IEntityManager entManager, IPrototypeManager protoManager)
    {
        var layers = new List<CEResourceIconLayer>();
        if (!protoManager.TryIndex(Skill, out var indexedSkill))
            return layers;

        layers.Add(CEResourceIconLayer.FromEntity(indexedSkill.Book) with { Offset = BookOffset });

        var skillSys = entManager.System<CESharedSkillSystem>();
        if (skillSys.GetSkillPreviewEntity(Skill) is { } preview)
            layers.Add(CEResourceIconLayer.FromEntity(preview.ID) with { Offset = SkillIconOffset });
        else if (skillSys.GetSkillIcon(Skill) is { } icon)
            layers.Add(CEResourceIconLayer.FromSprite(icon) with { Offset = SkillIconOffset });

        return layers;
    }

    private bool IsMatchingBook(IEntityManager entManager, EntityUid ent)
    {
        return entManager.TryGetComponent<CESkillBookComponent>(ent, out var book) && book.Skill == Skill;
    }
}
