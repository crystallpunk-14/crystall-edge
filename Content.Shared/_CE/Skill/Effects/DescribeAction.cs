using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill.Effects;

/// <summary>
/// Describes a skill using an action entity's name/description (the same fields <see cref="AddAction"/>
/// reads, including any cost text contributed via examine hooks), without granting anything. For
/// "theory" skills that should read like the spell they're theory for (e.g. in the science discovery
/// card, which sources its description from <see cref="CESharedSkillSystem.GetSkillEffectDescription"/>
/// - that only ever reads from an <see cref="Effect"/>, never <see cref="CESkillPrototype.PreviewEntity"/>)
/// without actually granting the action.
/// </summary>
public sealed partial class DescribeAction : CESkillEffect
{
    [DataField(required: true)]
    public EntProtoId Action;

    public override void AddSkill(IEntityManager entManager, EntityUid target)
    {
    }

    public override void RemoveSkill(IEntityManager entManager, EntityUid target)
    {
    }

    public override string? GetName(IEntityManager entManager, IPrototypeManager protoManager)
    {
        return !protoManager.TryIndex(Action, out var indexedAction) ? string.Empty : indexedAction.Name;
    }

    public override string? GetDescription(IEntityManager entManager, IPrototypeManager protoManager, ProtoId<CESkillPrototype> skill)
    {
        var dummyAction = entManager.Spawn(Action);
        var message = new FormattedMessage();
        if (!entManager.TryGetComponent<MetaDataComponent>(dummyAction, out var meta))
            return null;

        message.AddText(meta.EntityDescription + "\n");
        var ev = new ExaminedEvent(message, dummyAction, dummyAction, true, true);
        entManager.EventBus.RaiseLocalEvent(dummyAction, ev);

        entManager.DeleteEntity(dummyAction);
        return ev.GetTotalMessage().ToMarkup();
    }

    public override SpriteSpecifier? GetIcon(IEntityManager entManager, IPrototypeManager protoManager)
    {
        return null;
    }
}
