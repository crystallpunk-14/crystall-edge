using Content.Server._CE.EntitySlots;
using Content.Shared._CE.EntitySlots;
using Content.Shared._CE.Examine;
using Content.Shared.EntityConditions;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Whitelist;

namespace Content.Server._CE.AnimalHusbandry.Reproduction;

/// <summary>
/// Owns only prototype selection for fertilization and the incubation-host
/// interaction. Standard fixed slots and trigger effects own placement, time,
/// offspring spawning and product deletion.
/// </summary>
public sealed partial class CEAnimalIncubationSystem : EntitySystem
{
    [Dependency] private CEFixedEntitySlotSystem _fixedSlots = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEAnimalFertilizableProductComponent, CEFixedSlotEntityCreatingEvent>(OnProductCreating);
        SubscribeLocalEvent<CEAnimalIncubationHostComponent, AfterInteractUsingEvent>(OnHostInteractUsing);
        SubscribeLocalEvent<CEExamineAugmentEvent>(OnExamine);
    }

    private void OnProductCreating(
        Entity<CEAnimalFertilizableProductComponent> producer,
        ref CEFixedSlotEntityCreatingEvent args)
    {
        if (args.Cancelled || args.Prototype != producer.Comp.UnfertilizedPrototype)
            return;

        if (!IsConfigurationValid(producer.Comp) ||
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
        var map = Transform(host).MapUid;
        if (map == null || CountPopulation(map.Value, policy.PopulationWhitelist) >= policy.PopulationLimit)
            return false;

        if (policy.MateWhitelist == null)
            return true;

        foreach (var candidate in _lookup.GetEntitiesInRange(
                     Transform(host).Coordinates,
                     policy.FertilizationRange,
                     LookupFlags.Uncontained))
        {
            if (candidate == producer ||
                !_whitelist.IsValid(policy.MateWhitelist, candidate) ||
                !_conditions.TryConditions(candidate, policy.MateConditions, producer))
                continue;

            return true;
        }

        return false;
    }

    private int CountPopulation(EntityUid map, EntityWhitelist populationWhitelist)
    {
        var count = 0;
        var query = EntityQueryEnumerator<TransformComponent>();
        while (query.MoveNext(out var uid, out var transform))
        {
            if (transform.MapUid != map || !_whitelist.IsValid(populationWhitelist, uid))
                continue;

            if (TryComp<MobStateComponent>(uid, out var mobState) && _mobState.IsDead(uid, mobState))
                continue;

            count++;
        }

        return count;
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
        foreach (var product in _fixedSlots.GetOccupants((args.Examined, slots)))
        {
            if (product is not { } uid || !TryComp<CEAnimalIncubationComponent>(uid, out var incubation))
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
            ("capacity", _fixedSlots.Capacity((args.Examined, slots)))));
    }

    private static bool IsConfigurationValid(CEAnimalFertilizableProductComponent policy)
    {
        return float.IsFinite(policy.FertilizationRange) && policy.FertilizationRange >= 0f &&
            policy.PopulationWhitelist != null && policy.PopulationLimit > 0;
    }
}
