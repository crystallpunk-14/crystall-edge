using System.Linq;
using Content.Server._CE.Objectives.Target.Components;
using Content.Server.Chat.Managers;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Server.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CEManualTargetObjectiveComponent"/>.
/// </summary>
public sealed partial class CEManualTargetObjectiveSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private CETargetObjectiveSystem _targetObjective = default!;

    [SubscribeLocalEvent]
    private void OnInitializeObjective(Entity<CEManualTargetObjectiveComponent> ent, ref CEInitializeObjectiveEvent args)
    {
        ent.Comp.Holder = args.Holder.Owner;
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.Duration;
    }

    [SubscribeLocalEvent]
    private void OnAttemptAutoTarget(Entity<CEManualTargetObjectiveComponent> ent, ref CEAttemptAutoTargetEvent args)
    {
        args.Cancelled |= !ent.Comp.Expired;
    }

    [SubscribeLocalEvent]
    private void OnTargetChanged(Entity<CEManualTargetObjectiveComponent> ent, ref CEObjectiveTargetChangedEvent args)
    {
        if (ent.Comp.Finished || args.NewTarget is not { } target)
            return;

        ent.Comp.Finished = true;
        Hide(ent);

        if (!TryComp<MindComponent>(ent.Comp.Holder, out var mind) ||
            !_player.TryGetSessionById(mind.UserId, out var session))
        {
            return;
        }

        var message = Loc.GetString(ent.Comp.Message, ("targetName", Name(target)));
        _chat.ChatMessageToOne(ChatChannel.Server,
            message,
            FormattedMessage.EscapeText(message),
            EntityUid.Invalid,
            false,
            session.Channel);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CEManualTargetObjectiveComponent> ent, ref ComponentShutdown args)
    {
        Hide(ent);
    }

    [SubscribeLocalEvent]
    private void OnSelectTarget(CESelectManualTargetActionEvent args)
    {
        if (args.Handled)
            return;

        var query = EntityQueryEnumerator<CEManualTargetObjectiveComponent, CETargetObjectiveComponent>();
        while (query.MoveNext(out var uid, out var manual, out var targetComp))
        {
            if (manual.ActionEntity != args.Action.Owner ||
                !TryComp<CEObjectiveHolderComponent>(manual.Holder, out var holder))
            {
                continue;
            }

            // Deliberately unvalidated: rejecting someone for their secret role or objectives would reveal them.
            var candidates = _targetObjective.GetTargetCandidates((manual.Holder.Value, holder),
                (uid, targetComp),
                validate: false);

            if (!candidates.Contains(args.Target))
            {
                _popup.PopupEntity(Loc.GetString("ce-manual-target-invalid"), args.Performer, args.Performer);
                return;
            }

            args.Handled = true;
            _targetObjective.SetTarget((uid, targetComp), args.Target);
            return;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEManualTargetObjectiveComponent, CETargetObjectiveComponent>();
        while (query.MoveNext(out var uid, out var manual, out var targetComp))
        {
            if (manual.Finished || manual.Expired || manual.Holder is not { } holderUid)
                continue;

            if (_timing.CurTime >= manual.EndTime)
            {
                manual.Expired = true;
                Hide((uid, manual));

                if (TryComp<CEObjectiveHolderComponent>(holderUid, out var holder))
                    _targetObjective.TryAutoTarget((holderUid, holder), (uid, targetComp));

                continue;
            }

            // Follow the mind between bodies. Ghosts get nothing to pick with.
            var body = CompOrNull<MindComponent>(holderUid)?.OwnedEntity;
            if (HasComp<GhostComponent>(body))
                body = null;

            if (body == manual.Body)
                continue;

            Hide((uid, manual));

            if (body is { } newBody)
                Show((uid, manual), holderUid, newBody);
        }
    }

    private void Show(Entity<CEManualTargetObjectiveComponent> ent, EntityUid mindId, EntityUid body)
    {
        ent.Comp.Body = body;
        _actions.AddAction(body, ref ent.Comp.ActionEntity, ent.Comp.Action, mindId);
        _statusEffects.TrySetStatusEffectDuration(body, ent.Comp.StatusEffect, ent.Comp.EndTime - _timing.CurTime);
    }

    private void Hide(Entity<CEManualTargetObjectiveComponent> ent)
    {
        QueueDel(ent.Comp.ActionEntity);
        ent.Comp.ActionEntity = null;

        if (ent.Comp.Body is { } body && !TerminatingOrDeleted(body))
            _statusEffects.TryRemoveStatusEffect(body, ent.Comp.StatusEffect);

        ent.Comp.Body = null;
    }
}
