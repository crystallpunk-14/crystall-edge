using Content.Server._CE.Consumption;
using Content.Server._CE.GOAP.Navigation;
using Content.Server._CE.GOAP.Selectors;
using Content.Shared._CE.Consumption;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Consumption;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Consumption;

/// <summary>
/// Resolves a selected provider and performs one canonical ingestion operation
/// without owning provider-specific policy or movement.
/// </summary>
public sealed partial class CEGOAPConsumeActionSystem : CEGOAPActionSystem<CEGOAPConsumeAction>
{
    [Dependency] private CEGOAPTargetBackoffSystem _backoff = default!;
    [Dependency] private CEGOAPSelectorProfileSystem _selectorProfiles = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEGOAPConsumeComponent, IngestingEvent>(OnIngesting);
        SubscribeLocalEvent<CEGOAPConsumeComponent, EatingDoAfterEvent>(
            OnEatingDoAfter,
            after: [typeof(IngestionSystem)]);
        SubscribeLocalEvent<CEGOAPConsumeComponent, ComponentShutdown>(OnConsumptionShutdown);
    }

    protected override void OnCanExecute(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionCanExecuteEvent<CEGOAPConsumeAction> args)
    {
        if (!TryGetConfiguration(ent.Owner, args.Action, out var selector, out var source))
        {
            args.CanExecute = false;
            return;
        }

        if (TryComp<CEGOAPConsumeRetryComponent>(ent, out var retry))
        {
            if (retry.UntilBySource.TryGetValue(source, out var retryUntil) &&
                _timing.CurTime < retryUntil)
            {
                args.CanExecute = false;
                return;
            }

            retry.UntilBySource.Remove(source);
        }

        if (!TryResolveProvider(ent.Owner, selector, out _))
            args.CanExecute = false;
    }

    protected override void OnActionStartup(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionStartupEvent<CEGOAPConsumeAction> args)
    {
        if (!TryGetConfiguration(ent.Owner, args.Action, out _, out var source))
            return;

        var state = EnsureComp<CEGOAPConsumeComponent>(ent);
        state.Phase = CEGOAPConsumePhase.Acquiring;
        state.SourceDefinition = source;
        state.Provider = null;
        state.Consumable = null;
    }

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPConsumeAction> args)
    {
        if (!TryGetConfiguration(ent.Owner, args.Action, out var selector, out var source) ||
            !TryComp<CEGOAPConsumeComponent>(ent, out var state) ||
            !ReferenceEquals(state.SourceDefinition, source))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        switch (state.Phase)
        {
            case CEGOAPConsumePhase.Acquiring:
                if (!TryResolveProvider(ent.Owner, selector, out var provider))
                {
                    Fail(state);
                    args.Status = CEGOAPActionStatus.Failed;
                    return;
                }

                state.Provider = provider;
                if (!_interaction.InRangeAndAccessible(ent.Owner, provider))
                {
                    _backoff.Reject(ent.Owner, provider);
                    Fail(state);
                    args.Status = CEGOAPActionStatus.Failed;
                    return;
                }

                if (!state.SourceDefinition.TryResolveConsumable(
                        ent.Owner,
                        provider,
                        EntityManager,
                        out var consumable))
                {
                    Fail(state);
                    args.Status = CEGOAPActionStatus.Failed;
                    return;
                }

                state.Consumable = consumable;
                state.Phase = CEGOAPConsumePhase.Consuming;
                if (!_ingestion.TryIngest(ent.Owner, consumable))
                {
                    Fail(state);
                    args.Status = CEGOAPActionStatus.Failed;
                }
                return;

            case CEGOAPConsumePhase.Consuming:
                if (state.Consumable is not { } activeConsumable || Exists(activeConsumable))
                    return;

                Fail(state);
                args.Status = CEGOAPActionStatus.Failed;
                return;

            case CEGOAPConsumePhase.Finished:
                var refresh = new CEGOAPSensorRefreshEvent();
                RaiseLocalEvent(ent.Owner, ref refresh);
                args.Status = CEGOAPActionStatus.Finished;
                return;

            case CEGOAPConsumePhase.Failed:
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

        if (state.Phase == CEGOAPConsumePhase.Consuming && state.Consumable is { } consumable)
            CancelEatingDoAfter(ent.Owner, consumable);

        ReleaseConsumable(ent.Owner, state);

        if (state.Phase != CEGOAPConsumePhase.Finished && args.Action.RetryDelay > TimeSpan.Zero)
        {
            var retry = EnsureComp<CEGOAPConsumeRetryComponent>(ent);
            retry.UntilBySource[state.SourceDefinition] = _timing.CurTime + args.Action.RetryDelay;
        }

        RemComp<CEGOAPConsumeComponent>(ent);
    }

    private void ReleaseConsumable(EntityUid consumer, CEGOAPConsumeComponent state)
    {
        if (state.Provider is not { } provider || state.Consumable is not { } consumable)
            return;

        state.SourceDefinition.ReleaseConsumable(
            consumer,
            provider,
            consumable,
            state.Phase == CEGOAPConsumePhase.Finished,
            EntityManager);
        state.Provider = null;
        state.Consumable = null;
    }

    private void OnIngesting(Entity<CEGOAPConsumeComponent> ent, ref IngestingEvent args)
    {
        if (ent.Comp.Phase != CEGOAPConsumePhase.Consuming || ent.Comp.Consumable != args.Food)
            return;

        ent.Comp.Phase = CEGOAPConsumePhase.Finished;
    }

    private void OnEatingDoAfter(Entity<CEGOAPConsumeComponent> ent, ref EatingDoAfterEvent args)
    {
        if (args.Target is not { } source || ent.Comp.Consumable != source)
            return;

        if (ent.Comp.Phase == CEGOAPConsumePhase.Finished)
        {
            args.Repeat = false;
            return;
        }

        if (ent.Comp.Phase != CEGOAPConsumePhase.Consuming)
            return;

        Fail(ent.Comp);
    }

    private void OnConsumptionShutdown(Entity<CEGOAPConsumeComponent> ent, ref ComponentShutdown args)
    {
        ReleaseConsumable(ent.Owner, ent.Comp);
    }

    private void CancelEatingDoAfter(EntityUid consumer, EntityUid source)
    {
        if (!TryComp<DoAfterComponent>(consumer, out var doAfters))
            return;

        foreach (var doAfter in doAfters.DoAfters.Values)
        {
            if (doAfter.Cancelled || doAfter.Completed || doAfter.Args.Target != source ||
                doAfter.Args.Event is not EatingDoAfterEvent)
                continue;

            _doAfter.Cancel(consumer, doAfter.Index, doAfters);
            return;
        }
    }

    private static void Fail(CEGOAPConsumeComponent state)
    {
        state.Phase = CEGOAPConsumePhase.Failed;
    }

    private bool TryResolveProvider(
        EntityUid consumer,
        CEGOAPSelectorConsumableProvider selector,
        out EntityUid provider)
    {
        provider = default;
        if (selector.Resolve(consumer, EntityManager).Entity is not { } resolved)
            return false;

        provider = resolved;
        return true;
    }

    private bool TryGetConfiguration(
        EntityUid consumer,
        CEGOAPConsumeAction action,
        out CEGOAPSelectorConsumableProvider selector,
        out CEConsumableSource source)
    {
        selector = null!;
        source = null!;
        if (!_selectorProfiles.TryResolveSelector(consumer, action.Selector, out var resolved) ||
            resolved is not CEGOAPSelectorConsumableProvider resolvedSelector ||
            resolvedSelector.Source == null ||
            !float.IsFinite(resolvedSelector.Range) || resolvedSelector.Range < 0f ||
            action.RetryDelay < TimeSpan.Zero)
            return false;

        selector = resolvedSelector;
        source = resolvedSelector.Source;
        return true;
    }
}
