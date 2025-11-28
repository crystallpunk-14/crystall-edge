using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill.Effects;

public sealed partial class AddAction : CESkillEffect
{
    [DataField(required: true)]
    public EntProtoId Action;

    public override void AddSkill(IEntityManager entManager, EntityUid target)
    {
        var actionsSystem = entManager.System<SharedActionsSystem>();
        var actionsContainerSys = entManager.System<ActionContainerSystem>();
        var mindSys = entManager.System<SharedMindSystem>();

        if (!mindSys.TryGetMind(target, out var mind, out _))
            actionsSystem.AddAction(target, Action);
        else
            actionsContainerSys.AddAction(mind, Action);
    }

    public override void RemoveSkill(IEntityManager entManager, EntityUid target)
    {
        var actionsSystem = entManager.System<SharedActionsSystem>();
        var actionsContainerSys = entManager.System<ActionContainerSystem>();
        var mindSys = entManager.System<SharedMindSystem>();

        foreach (var (uid, _) in actionsSystem.GetActions(target))
        {
            if (!entManager.TryGetComponent<MetaDataComponent>(uid, out var metaData))
                continue;

            if (metaData.EntityPrototype == null)
                continue;

            if (metaData.EntityPrototype != Action)
                continue;

            if (!mindSys.TryGetMind(target, out var mind, out _))
                actionsSystem.RemoveAction(target, uid);
            else
                actionsContainerSys.RemoveAction(uid);
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
}
