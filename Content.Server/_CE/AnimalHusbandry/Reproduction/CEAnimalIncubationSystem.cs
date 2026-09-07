using Content.Server._CE.EntitySlots;
using Content.Shared._CE.EntitySlots;
using Content.Shared._CE.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Whitelist;

namespace Content.Server._CE.AnimalHusbandry.Reproduction;

/// <summary>
/// Owns fertility accounting, product selection and the incubation-host
/// interaction. Standard fixed slots and trigger effects own placement, time,
/// offspring spawning and product deletion.
/// </summary>
public sealed partial class CEAnimalIncubationSystem : EntitySystem
{
    [Dependency] private CEFixedEntitySlotSystem _fixedSlots = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEAnimalFertilizableProductComponent, CEFixedSlotEntityCreatingEvent>(OnProductCreating);
        SubscribeLocalEvent<CEAnimalFertilityComponent, CEFixedSlotEntityProducedEvent>(OnProduced);
        SubscribeLocalEvent<CEAnimalIncubationHostComponent, AfterInteractUsingEvent>(OnHostInteractUsing);
        SubscribeLocalEvent<CEExamineAugmentEvent>(OnExamine);
    }

    private void OnProductCreating(
        Entity<CEAnimalFertilizableProductComponent> producer,
        ref CEFixedSlotEntityCreatingEvent args)
    {
        if (args.Cancelled || args.Prototype != producer.Comp.UnfertilizedPrototype)
            return;

        if (producer.Comp.PopulationWhitelist == null || producer.Comp.PopulationLimit <= 0 ||
            producer.Comp.UnfertilizedPrototype == producer.Comp.FertilizedPrototype ||
            !HasComp<CEAnimalIncubationHostComponent>(args.Target))
        {
            args.Cancelled = true;
            return;
        }

        if (CanFertilize(args.Target, producer.Owner, producer.Comp))
            args.Prototype = producer.Comp.FertilizedPrototype;
    }

    private bool CanFertilize(
        EntityUid host,
        EntityUid producer,
        CEAnimalFertilizableProductComponent policy)
    {
        if (!TryComp<CEAnimalFertilityComponent>(producer, out var fertility) || fertility.ProductsRemaining <= 0)
            return false;

        var map = Transform(host).MapUid;
        return map != null && !IsPopulationAtLimit(map.Value, policy);
    }

    private void OnProduced(Entity<CEAnimalFertilityComponent> ent, ref CEFixedSlotEntityProducedEvent args)
    {
        // Creating only chooses the prototype; spend fertility after the slot transaction has committed.
        if (TryComp<CEAnimalIncubationComponent>(args.Product, out var incubation) && incubation.Fertilized)
            ent.Comp.ProductsRemaining = Math.Max(0, ent.Comp.ProductsRemaining - 1);
    }

    private bool IsPopulationAtLimit(EntityUid map, CEAnimalFertilizableProductComponent policy)
    {
        var count = 0;
        var query = EntityQueryEnumerator<TransformComponent>();
        while (query.MoveNext(out var uid, out var transform))
        {
            if (transform.MapUid != map || !_whitelist.IsValid(policy.PopulationWhitelist, uid))
                continue;

            if (TryComp<MobStateComponent>(uid, out var mobState) && _mobState.IsDead(uid, mobState))
                continue;

            if (++count >= policy.PopulationLimit)
                return true;
        }

        return false;
    }

    private void OnHostInteractUsing(
        Entity<CEAnimalIncubationHostComponent> ent,
        ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach ||
            !HasComp<CEAnimalIncubationComponent>(args.Used) ||
            !TryComp<CEFixedEntitySlotsComponent>(ent.Owner, out var slots) ||
            !_fixedSlots.TryInsertFromHand(args.User, args.Used, (ent.Owner, slots), out _))
            return;

        args.Handled = true;
    }

    private void OnExamine(CEExamineAugmentEvent args)
    {
        if (!TryComp<CEAnimalIncubationHostComponent>(args.Examined, out var host) ||
            !TryComp<CEFixedEntitySlotsComponent>(args.Examined, out var slots))
            return;

        var ordinary = 0;
        var fertilized = 0;
        for (var slot = 0; slot < slots.Slots.Count; slot++)
        {
            if (!_fixedSlots.TryGetOccupant((args.Examined, slots), slot, out var product) ||
                !TryComp<CEAnimalIncubationComponent>(product, out var incubation))
                continue;

            if (incubation.Fertilized)
                fertilized++;
            else
                ordinary++;
        }

        args.AddMarkup(Loc.GetString(
            host.ExamineMessage,
            ("ordinary", ordinary),
            ("fertilized", fertilized),
            ("capacity", slots.Slots.Count)));
    }

}
