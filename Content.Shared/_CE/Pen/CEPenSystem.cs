using Content.Shared._CE.Paper;
using Content.Shared.Interaction;
using Content.Shared.UserInterface;

namespace Content.Shared._CE.Pen;

/// <summary>
/// Drives "smart pens" (<see cref="CEPenComponent"/>): using one on another entity collects
/// available pen actions via <see cref="CEGetPenActionsEvent"/> from the user, the target and
/// the pen itself, then either performs the single available action directly, or lets the
/// player pick from a radial menu.
/// </summary>
public sealed partial class CEPenSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private CEPaperSystem _paper = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEPenComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CEPenComponent, CEPenActionsMessage>(OnPenActionsMessage);
    }

    private void OnAfterInteract(Entity<CEPenComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        var actions = CollectActions(args.User, ent.Owner, target);
        if (actions.Count == 0)
            return;

        args.Handled = true;
        ent.Comp.PendingTarget = target;

        // A lone "write" action needs no menu - anything else (including a lone "record
        // knowledge" action, which still needs the achievement submenu) opens the radial menu.
        if (actions.Count == 1 && actions[0].Kind == CEPenActionKind.Write)
        {
            _paper.TryWrite(target, args.User, ent.Owner);
            return;
        }

        _ui.OpenUi(ent.Owner, CEPenActionsUiKey.Key, args.User);
    }

    /// <summary>
    /// Raises <see cref="CEGetPenActionsEvent"/> on the user, the target and the pen (in that
    /// order), collecting every pen action any of the three want to offer.
    /// </summary>
    public List<CEPenAction> CollectActions(EntityUid user, EntityUid pen, EntityUid target)
    {
        var ev = new CEGetPenActionsEvent(user, pen, target);
        RaiseLocalEvent(user, ev);
        RaiseLocalEvent(target, ev);
        RaiseLocalEvent(pen, ev);
        return ev.Actions;
    }

    private void OnPenActionsMessage(Entity<CEPenComponent> ent, ref CEPenActionsMessage args)
    {
        if (ent.Comp.PendingTarget is not { } target)
            return;

        switch (args.Kind)
        {
            case CEPenActionKind.Write:
                _paper.TryWrite(target, args.Actor, ent.Owner);
                break;
            case CEPenActionKind.RecordKnowledge:
                if (args.Achievement is { } achievement)
                    RaiseLocalEvent(args.Actor, new CEPenRecordKnowledgeRequestEvent(ent.Owner, target, achievement));
                break;
        }
    }
}
