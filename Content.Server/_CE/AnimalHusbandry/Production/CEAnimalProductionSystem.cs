using Content.Server._CE.Actions;
using Content.Server._CE.AnimalHusbandry.Reproduction;
using Content.Server._CE.GOAP;
using Content.Shared._CE.AnimalHusbandry.Reproduction;
using Content.Shared._CE.GOAP;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.EntityConditions;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.AnimalHusbandry.Production;

/// <summary>Owns animal production, fertility selection and committing a laid product to a native nest slot.</summary>
public sealed partial class CEAnimalProductionSystem : EntitySystem
{
    [Dependency] private CEGrantedActionResolverSystem _resolver = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private CEAnimalNestSystem _nests = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEAnimalProductionComponent> ent, ref MapInitEvent args)
    {
        var p = ent.Comp;
        if (p.FirstMinimum < TimeSpan.Zero || p.FirstMaximum < p.FirstMinimum ||
            p.RepeatMinimum < TimeSpan.Zero || p.RepeatMaximum < p.RepeatMinimum ||
            p.PollInterval <= TimeSpan.Zero || !float.IsFinite(p.HungerCost) || p.HungerCost < 0 ||
            p.PopulationLimit <= 0 || p.ProductPrototype == p.FertilizedPrototype)
        {
            p.Disabled = true;
            Log.Error($"Invalid animal production configuration on {ToPrettyString(ent)}.");
            return;
        }

        if (!p.WaitingForOutputSpend && p.NextProductionAt == TimeSpan.Zero)
            Schedule(ent, p.FirstMinimum, p.FirstMaximum);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<CEAnimalProductionComponent>();
        while (query.MoveNext(out var uid, out var producer))
        {
            if (producer.Disabled || _timing.CurTime < producer.NextPollAt)
                continue;
            if (!producer.WaitingForOutputSpend && _timing.CurTime < producer.NextProductionAt)
            {
                producer.NextPollAt = producer.NextProductionAt;
                continue;
            }
            producer.NextPollAt = _timing.CurTime + producer.PollInterval;
            if (!TryResolveAction(uid, producer, out var action))
                continue;

            var charges = _charges.GetCurrentCharges((action.Owner, action.Comp, null));
            if (producer.WaitingForOutputSpend)
            {
                if (charges != 0)
                    continue;
                producer.WaitingForOutputSpend = false;
                Schedule((uid, producer), producer.RepeatMinimum, producer.RepeatMaximum);
                Refresh(uid);
                continue;
            }

            if (charges > 0)
            {
                producer.WaitingForOutputSpend = true;
                Refresh(uid);
                continue;
            }

            if (charges != 0 || action.Comp.MaxCharges != 1 ||
                !_conditions.TryConditions(uid, producer.Conditions) ||
                !TryComp<SatiationComponent>(uid, out var needs) ||
                _satiation.GetValueOrNull((uid, needs), SatiationSystem.Hunger) is not { } hunger ||
                !float.IsFinite(hunger) || hunger < producer.HungerCost)
                continue;

            // This is a bounded Satiation debit, not an arbitrary EntityEffect success contract.
            var remaining = hunger - producer.HungerCost;
            _satiation.SetValue((uid, needs), SatiationSystem.Hunger, remaining);
            if (_satiation.GetValueOrNull((uid, needs), SatiationSystem.Hunger) is not { } paid ||
                Math.Abs(paid - remaining) > 0.001f || TerminatingOrDeleted(uid) || TerminatingOrDeleted(action))
            {
                if (!TerminatingOrDeleted(uid))
                    _satiation.SetValue((uid, needs), SatiationSystem.Hunger, hunger);
                continue;
            }

            _charges.AddCharges((action.Owner, action.Comp, null), 1);
            if (_charges.GetCurrentCharges((action.Owner, action.Comp, null)) != 1)
            {
                _satiation.SetValue((uid, needs), SatiationSystem.Hunger, hunger);
                producer.Disabled = true;
                Log.Error($"Failed to grant animal production charge on {ToPrettyString(uid)}.");
                continue;
            }
            producer.WaitingForOutputSpend = true;
            Refresh(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnLay(Entity<CEAnimalProductionComponent> ent, ref CEAnimalLayProductActionEvent args)
    {
        if (args.Handled || ent.Comp.Disabled ||
            !TryResolveAction(ent, ent.Comp, out var action) || action.Owner != args.Action.Owner ||
            !_charges.HasCharges((action.Owner, action.Comp), 1) ||
            !TryComp<EntityTargetActionComponent>(args.Action, out var targetAction) ||
            !_actions.ValidateEntityTarget(ent, args.Target, (args.Action.Owner, targetAction)) ||
            !TryComp<CEAnimalNestComponent>(args.Target, out var nest) ||
            !TryComp<ItemSlotsComponent>(args.Target, out var slots))
            return;

        _nests.RefreshAvailability((args.Target, nest));
        if (!HasComp<CEAnimalNestAvailableComponent>(args.Target))
            return;

        var fertile = TryComp<CEAnimalFertilityComponent>(ent, out var fertility) && fertility.ProductsRemaining > 0 &&
                      Transform(args.Target).MapUid is { } map && !PopulationAtLimit(map, ent.Comp);
        var prototype = fertile ? ent.Comp.FertilizedPrototype : ent.Comp.ProductPrototype;
        EntityUid product;
        try
        {
            product = SpawnAtPosition(prototype, Transform(args.Target).Coordinates);
        }
        catch (Exception exception)
        {
            Log.Warning($"Could not lay {prototype} for {ToPrettyString(ent)}: {exception.Message}");
            return;
        }

        if (!_slots.TryGetAvailableSlot((args.Target, slots), product, null, out var slot, emptyOnly: true) ||
            !_slots.TryInsert(args.Target, slot, product, null))
        {
            QueueDel(product);
            return;
        }

        // Native insertion has committed. Failed spawn/insertion never spends fertility or the action charge.
        if (fertile)
            fertility!.ProductsRemaining--;
        _nests.RestAfterLaying(ent, (args.Target, nest));
        args.Handled = true; // SharedChargesSystem spends the pending output on ActionPerformedEvent.
    }

    private bool PopulationAtLimit(EntityUid map, CEAnimalProductionComponent policy)
    {
        var count = 0;
        var query = AllEntityQuery<TransformComponent>();
        while (query.MoveNext(out var uid, out var transform))
        {
            if (TerminatingOrDeleted(uid) || transform.MapUid != map || !_whitelist.IsValid(policy.PopulationWhitelist, uid) ||
                TryComp<MobStateComponent>(uid, out var mob) && _mobState.IsDead(uid, mob))
                continue;
            if (++count >= policy.PopulationLimit)
                return true;
        }
        return false;
    }

    private bool TryResolveAction(EntityUid uid, CEAnimalProductionComponent producer, out Entity<LimitedChargesComponent> action)
    {
        action = default;
        if (!_resolver.TryResolveUnique(uid, producer.OutputAction, out var granted) ||
            !TryComp<LimitedChargesComponent>(granted, out var charges) || HasComp<AutoRechargeComponent>(granted))
            return false;
        action = (granted.Owner, charges);
        return true;
    }

    private void Schedule(Entity<CEAnimalProductionComponent> ent, TimeSpan minimum, TimeSpan maximum)
    {
        ent.Comp.NextProductionAt = _timing.CurTime + _random.Next(minimum, maximum);
        ent.Comp.NextPollAt = ent.Comp.NextProductionAt;
    }

    private void Refresh(EntityUid uid)
    {
        var ev = new CEGOAPSensorRefreshEvent();
        RaiseLocalEvent(uid, ref ev);
    }
}
