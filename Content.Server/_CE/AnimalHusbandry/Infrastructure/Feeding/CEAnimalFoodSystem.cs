using Content.Server._CE.AnimalHusbandry.Resources.Consumption;
using Content.Server._CE.EntitySlots;
using Content.Server.Stack;
using Content.Shared._CE.Examine;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Server._CE.AnimalHusbandry.Infrastructure.Feeding;

/// <summary>
/// Applies feed policy to reusable fixed slots. Upstream ingestion performs the actual bite.
/// </summary>
public sealed partial class CEAnimalFoodSystem : EntitySystem
{
    [Dependency] private CEConnectedEntitySlotsSystem _connectedSlots = default!;
    [Dependency] private CEAnimalDietSystem _diet = default!;
    [Dependency] private CEFixedEntitySlotSystem _fixedSlots = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private StackSystem _stacks = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEFeedTroughComponent, AfterInteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CEFeedTroughComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
        SubscribeLocalEvent<CEExamineAugmentEvent>(OnExamine);
    }

    public bool CanProvideFood(Entity<CEFeedTroughComponent> trough, EntityUid consumer)
    {
        return TryComp<CEFixedEntitySlotsComponent>(trough.Owner, out var slots) &&
            TryGetFood(trough, (trough.Owner, slots), consumer, out _);
    }

    public bool TryTakeFood(EntityUid troughUid, EntityUid consumer, out EntityUid food)
    {
        food = default;
        if (!TryComp<CEFeedTroughComponent>(troughUid, out var trough) ||
            !TryComp<CEFixedEntitySlotsComponent>(troughUid, out var slots) ||
            !TryGetFood((troughUid, trough), (troughUid, slots), consumer, out food))
            return false;

        if (trough.ReservedFood.TryGetValue(food, out var owner))
            return owner == consumer;

        return trough.ReservedFood.TryAdd(food, consumer);
    }

    public bool ReleaseFood(EntityUid troughUid, EntityUid consumer, EntityUid food)
    {
        if (!TryComp<CEFeedTroughComponent>(troughUid, out var trough) ||
            !trough.ReservedFood.TryGetValue(food, out var owner) ||
            owner != consumer)
            return false;

        return trough.ReservedFood.Remove(food);
    }

    private bool TryGetFood(
        Entity<CEFeedTroughComponent> trough,
        Entity<CEFixedEntitySlotsComponent> slots,
        EntityUid consumer,
        out EntityUid food)
    {
        food = default;
        List<EntityUid>? staleReservations = null;
        foreach (var (reservedFood, owner) in trough.Comp.ReservedFood)
        {
            if (Exists(reservedFood) && Exists(owner))
                continue;

            (staleReservations ??= new()).Add(reservedFood);
        }

        if (staleReservations != null)
        {
            foreach (var stale in staleReservations)
                trough.Comp.ReservedFood.Remove(stale);
        }

        EntityUid? available = null;
        for (var slot = 0; slot < slots.Comp.Slots.Count; slot++)
        {
            if (!_fixedSlots.TryGetOccupant(slots, slot, out var candidate))
                continue;

            if (trough.Comp.ReservedFood.TryGetValue(candidate, out var owner))
            {
                if (owner != consumer || !_diet.CanSelectFood(consumer, (candidate, null)))
                    continue;

                // A consumer must see its own claim through every independently
                // deserialized source-strategy instance.
                food = candidate;
                return true;
            }

            if (available != null || !_diet.CanSelectFood(consumer, (candidate, null)))
                continue;

            available = candidate;
        }

        if (available is not { } unreserved)
            return false;

        food = unreserved;
        return true;
    }

    private void OnContainerRemoved(Entity<CEFeedTroughComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        ent.Comp.ReservedFood.Remove(args.Entity);
    }

    private void OnInteractUsing(Entity<CEFeedTroughComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach ||
            !TryComp<EdibleComponent>(args.Used, out var edible) ||
            _ingestion.GetEdibleType((args.Used, edible)) != IngestionSystem.Food ||
            _ingestion.TotalNutrition((args.Used, edible)) <= 0f)
            return;

        if (!_connectedSlots.TryInsertFromHand(args.User, args.Used, ent.Owner, out _, out _))
            return;

        args.Handled = true;
    }

    private void OnExamine(CEExamineAugmentEvent args)
    {
        if (!TryComp<CEFeedTroughComponent>(args.Examined, out var trough))
            return;

        var portions = 0;
        var nutrition = 0f;
        foreach (var member in _connectedSlots.GetMembersByDistance(args.Examined))
        {
            if (!TryComp<CEFixedEntitySlotsComponent>(member, out var slots))
                continue;

            for (var slot = 0; slot < slots.Slots.Count; slot++)
            {
                if (!_fixedSlots.TryGetOccupant((member, slots), slot, out var food) ||
                    !TryComp<EdibleComponent>(food, out var edible))
                    continue;

                var count = _stacks.GetCount((food, null));
                portions += count;
                nutrition += _ingestion.TotalNutrition((food, edible)) * count;
            }
        }

        args.AddMarkup(Loc.GetString(
            trough.ExamineMessage,
            ("portions", portions),
            ("nutrition", Math.Round(nutrition, trough.NutritionPrecision))));
    }
}
