using Content.Server._CE.Actions;
using Content.Server._CE.GOAP;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Production;

/// <summary>
/// Produces a pending discrete output by adding a charge to a configured granted action.
/// </summary>
public sealed partial class CEProductionAccumulatorSystem : EntitySystem
{
    [Dependency] private CEGrantedActionResolverSystem _actionResolver = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<LimitedChargesComponent> _limitedChargesQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEProductionAccumulatorComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEProductionAccumulatorComponent>();
        while (query.MoveNext(out var uid, out var accumulator))
        {
            if (accumulator.Disabled)
                continue;

            var currentTime = _timing.CurTime;
            if (currentTime < accumulator.NextPollAt)
                continue;

            if (accumulator.PollInterval <= TimeSpan.Zero)
            {
                Disable((uid, accumulator),
                    $"Invalid production poll interval {accumulator.PollInterval}");
                continue;
            }

            if (!accumulator.WaitingForOutputSpend && currentTime < accumulator.NextProductionAt)
            {
                accumulator.NextPollAt = accumulator.NextProductionAt;
                continue;
            }

            accumulator.NextPollAt = currentTime + accumulator.PollInterval;

            if (!TryResolveOutputAction(uid, accumulator, out var action))
                continue;

            var chargeEntity = new Entity<LimitedChargesComponent?, AutoRechargeComponent?>(
                action.Owner,
                action.Comp,
                null);
            var currentCharges = _charges.GetCurrentCharges(chargeEntity);
            if (currentCharges < 0)
                continue;

            if (accumulator.WaitingForOutputSpend)
            {
                if (currentCharges > 0)
                    continue;

                accumulator.WaitingForOutputSpend = false;
                TrySchedule((uid, accumulator), accumulator.RepeatMinimum, accumulator.RepeatMaximum);
                RefreshSensors(uid);
                continue;
            }

            // A charge added by another authoritative path is still the pending output.
            if (currentCharges > 0)
            {
                accumulator.WaitingForOutputSpend = true;
                accumulator.NextProductionAt = TimeSpan.Zero;
                RefreshSensors(uid);
                continue;
            }

            TryAccumulate((uid, accumulator), action, chargeEntity);
        }
    }

    private void OnMapInit(Entity<CEProductionAccumulatorComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Disabled)
            return;

        if (!IsConfigurationValid(ent.Comp))
        {
            Disable(ent, "Invalid production accumulator configuration");
            return;
        }

        if (ent.Comp.NextProductionAt == TimeSpan.Zero && !ent.Comp.WaitingForOutputSpend)
            TrySchedule(ent, ent.Comp.FirstMinimum, ent.Comp.FirstMaximum);
        else if (ent.Comp.NextPollAt == TimeSpan.Zero)
            ent.Comp.NextPollAt = ent.Comp.WaitingForOutputSpend
                ? _timing.CurTime + ent.Comp.PollInterval
                : ent.Comp.NextProductionAt;
    }

    private bool TryResolveOutputAction(
        EntityUid producer,
        CEProductionAccumulatorComponent accumulator,
        out Entity<LimitedChargesComponent> action)
    {
        action = default;
        if (!_actionResolver.TryResolveUnique(producer, accumulator.OutputAction, out var granted) ||
            !_limitedChargesQuery.TryComp(granted, out var charges) ||
            HasComp<AutoRechargeComponent>(granted))
            return false;

        action = (granted.Owner, charges);
        return true;
    }

    private void TryAccumulate(
        Entity<CEProductionAccumulatorComponent> ent,
        Entity<LimitedChargesComponent> action,
        Entity<LimitedChargesComponent?, AutoRechargeComponent?> chargeEntity)
    {
        if (!IsConfigurationValid(ent.Comp))
        {
            Disable(ent, "Invalid production accumulator configuration");
            return;
        }

        if (action.Comp.MaxCharges <= 0 ||
            !_conditions.TryConditions(ent.Owner, ent.Comp.Conditions))
            return;

        var previousCharges = _charges.GetCurrentCharges(chargeEntity);
        if (previousCharges < 0 || previousCharges >= action.Comp.MaxCharges)
            return;

        if (ent.Comp.InputCost is { } inputCost &&
            !_effects.TryApplyEffect(ent.Owner, inputCost, user: ent.Owner))
            return;

        if (!Exists(ent) || !Exists(action))
            return;

        _charges.AddCharges(chargeEntity, 1);
        if (_charges.GetCurrentCharges(chargeEntity) <= previousCharges)
        {
            Disable(ent,
                $"Failed to add production charge to {ToPrettyString(action)}");
            return;
        }

        ent.Comp.WaitingForOutputSpend = true;
        ent.Comp.NextProductionAt = TimeSpan.Zero;
        RefreshSensors(ent);
    }

    private bool TrySchedule(
        Entity<CEProductionAccumulatorComponent> ent,
        TimeSpan minimum,
        TimeSpan maximum)
    {
        if (minimum < TimeSpan.Zero || maximum < minimum)
        {
            Disable(ent, $"Invalid production interval [{minimum}, {maximum}]");
            return false;
        }

        ent.Comp.NextProductionAt = _timing.CurTime + _random.Next(minimum, maximum);
        ent.Comp.NextPollAt = ent.Comp.NextProductionAt;
        return true;
    }

    private void Disable(Entity<CEProductionAccumulatorComponent> ent, string reason)
    {
        ent.Comp.Disabled = true;
        ent.Comp.WaitingForOutputSpend = false;
        ent.Comp.NextProductionAt = TimeSpan.Zero;
        ent.Comp.NextPollAt = TimeSpan.Zero;
        Log.Error($"{reason} on {ToPrettyString(ent)}.");
    }

    private static bool IsConfigurationValid(CEProductionAccumulatorComponent accumulator)
    {
        return accumulator.FirstMinimum >= TimeSpan.Zero &&
            accumulator.FirstMaximum >= accumulator.FirstMinimum &&
            accumulator.RepeatMinimum >= TimeSpan.Zero &&
            accumulator.RepeatMaximum >= accumulator.RepeatMinimum &&
            accumulator.PollInterval > TimeSpan.Zero;
    }

    private void RefreshSensors(EntityUid producer)
    {
        var ev = new CEGOAPSensorRefreshEvent();
        RaiseLocalEvent(producer, ref ev);
    }
}
