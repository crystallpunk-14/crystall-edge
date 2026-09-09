using Content.Server._CE.GOAP.Navigation;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Consumption;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Consumption;

/// <summary>
/// Performs one native ingestion operation on the selected edible. IngestionSystem
/// owns the DoAfter, solution transfer and needs; this system owns only GOAP lifecycle.
/// </summary>
public sealed partial class CEGOAPConsumeActionSystem : CEGOAPActionSystem<CEGOAPConsumeAction>
{
    [Dependency] private CEGOAPTargetBackoffSystem _backoff = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private IGameTiming _timing = default!;

    protected override void OnCanExecute(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionCanExecuteEvent<CEGOAPConsumeAction> args)
    {
        if (args.Action.RetryDelay < TimeSpan.Zero)
        {
            args.CanExecute = false;
            return;
        }

        if (TryComp<CEGOAPConsumeRetryComponent>(ent, out var retry))
        {
            if (retry.UntilByAction.TryGetValue(args.Action, out var until) && _timing.CurTime < until)
            {
                args.CanExecute = false;
                return;
            }

            retry.UntilByAction.Remove(args.Action);
        }

        if (!TryResolveEdible(ent.Owner, args.Action, out _))
            args.CanExecute = false;
    }

    protected override void OnActionStartup(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionStartupEvent<CEGOAPConsumeAction> args)
    {
        var state = EnsureComp<CEGOAPConsumeComponent>(ent);
        state.Phase = CEGOAPConsumePhase.Failed;
        state.Target = null;
        if (!TryResolveEdible(ent.Owner, args.Action, out var target))
            return;

        // A newly appearing closer food must not redirect an in-progress bite.
        state.Target = target;
        state.Phase = CEGOAPConsumePhase.Acquiring;
    }

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPConsumeAction> args)
    {
        if (!TryComp<CEGOAPConsumeComponent>(ent, out var state))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        args.Target = state.Target;
        switch (state.Phase)
        {
            case CEGOAPConsumePhase.Acquiring:
                if (state.Target is not { } target || !Exists(target) ||
                    !_interaction.InRangeAndAccessible(ent.Owner, target))
                {
                    if (state.Target is { } rejected)
                        _backoff.Reject(ent.Owner, rejected);
                    state.Phase = CEGOAPConsumePhase.Failed;
                    args.Status = CEGOAPActionStatus.Failed;
                    return;
                }

                // Set this before TryIngest, which can finish a zero-delay bite immediately.
                state.Phase = CEGOAPConsumePhase.Consuming;
                if (!_ingestion.TryIngest(ent.Owner, target))
                {
                    _backoff.Reject(ent.Owner, target);
                    state.Phase = CEGOAPConsumePhase.Failed;
                    args.Status = CEGOAPActionStatus.Failed;
                }
                return;

            case CEGOAPConsumePhase.Consuming:
                if (state.Target is { } active && Exists(active))
                    return;

                state.Phase = CEGOAPConsumePhase.Failed;
                args.Status = CEGOAPActionStatus.Failed;
                return;

            case CEGOAPConsumePhase.Finished:
                var refresh = new CEGOAPSensorRefreshEvent();
                RaiseLocalEvent(ent.Owner, ref refresh);
                args.Status = CEGOAPActionStatus.Finished;
                return;

            default:
                args.Status = CEGOAPActionStatus.Failed;
                return;
        }
    }

    protected override void OnActionShutdown(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionShutdownEvent<CEGOAPConsumeAction> args)
    {
        if (!TryComp<CEGOAPConsumeComponent>(ent, out var state))
            return;

        CancelEating(ent.Owner, state);
        if (state.Phase != CEGOAPConsumePhase.Finished && args.Action.RetryDelay > TimeSpan.Zero)
        {
            var retry = EnsureComp<CEGOAPConsumeRetryComponent>(ent);
            retry.UntilByAction[args.Action] = _timing.CurTime + args.Action.RetryDelay;
        }

        RemComp<CEGOAPConsumeComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnIngesting(Entity<CEGOAPConsumeComponent> ent, ref IngestingEvent args)
    {
        if (ent.Comp.Phase == CEGOAPConsumePhase.Consuming && ent.Comp.Target == args.Food)
            ent.Comp.Phase = args.Split.Volume > FixedPoint2.Zero
                ? CEGOAPConsumePhase.Finished
                : CEGOAPConsumePhase.Failed;
    }

    [SubscribeLocalEvent(after: [typeof(IngestionSystem)])]
    private void OnEatingDoAfter(Entity<CEGOAPConsumeComponent> ent, ref EatingDoAfterEvent args)
    {
        if (ent.Comp.Target != args.Target)
            return;

        // One bite per action; the refreshed needs sensor decides whether another is needed.
        args.Repeat = false;
        if (ent.Comp.Phase == CEGOAPConsumePhase.Consuming)
            ent.Comp.Phase = CEGOAPConsumePhase.Failed;
    }

    [SubscribeLocalEvent]
    private void OnConsumptionShutdown(Entity<CEGOAPConsumeComponent> ent, ref ComponentShutdown args)
    {
        CancelEating(ent.Owner, ent.Comp);
    }

    private void CancelEating(EntityUid consumer, CEGOAPConsumeComponent state)
    {
        if (state.Phase != CEGOAPConsumePhase.Consuming || state.Target is not { } target ||
            !TryComp<DoAfterComponent>(consumer, out var doAfters))
            return;

        foreach (var doAfter in doAfters.DoAfters.Values)
        {
            if (doAfter.Cancelled || doAfter.Completed || doAfter.Args.Target != target ||
                doAfter.Args.Event is not EatingDoAfterEvent)
                continue;

            _doAfter.Cancel(consumer, doAfter.Index, doAfters);
            return;
        }
    }

    private bool TryResolveEdible(EntityUid consumer, CEGOAPConsumeAction action, out EntityUid target)
    {
        target = default;
        if (action.Selector?.Resolve(consumer, EntityManager).Entity is not { } selected ||
            !HasComp<EdibleComponent>(selected) || !_interaction.IsAccessible(consumer, selected) ||
            !_ingestion.CanIngest(consumer, selected) || !_ingestion.CanConsume(consumer, selected))
            return false;

        target = selected;
        return true;
    }
}
