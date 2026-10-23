using System.Linq;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill.Effects;

public sealed partial class AddAction : CESkillEffect
{
    [DataField(required: true)]
    public EntProtoId Action;

    public override void AddSkill(IEntityManager entManager, EntityUid target)
    {
        entManager.System<SharedActionsSystem>().AddAction(target, Action);
    }

    public override void RemoveSkill(IEntityManager entManager, EntityUid target)
    {
        var actionsSystem = entManager.System<SharedActionsSystem>();

        foreach (var (uid, _) in actionsSystem.GetActions(target).ToList())
        {
            if (!entManager.TryGetComponent<MetaDataComponent>(uid, out var metaData))
                continue;

            if (metaData.EntityPrototype == null)
                continue;

            if (metaData.EntityPrototype != Action)
                continue;

            actionsSystem.RemoveAction(target, uid);
        }
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
