using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Server._CE.AnimalHusbandry.Lifecycle;

/// <summary>Accumulates healthy active time and permanently replaces the animal when ready.</summary>
public sealed partial class CEAnimalGrowthSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        if (frameTime <= 0 || !float.IsFinite(frameTime))
            return;

        List<Entity<CEAnimalGrowthComponent>>? ready = null;
        var query = EntityQueryEnumerator<CEAnimalGrowthComponent>();
        while (query.MoveNext(out var uid, out var growth))
        {
            growth.RetryRemaining = TimeSpan.FromSeconds(Math.Max(0, growth.RetryRemaining.TotalSeconds - frameTime));
            if (growth.RetryRemaining > TimeSpan.Zero)
                continue;
            if (!_conditions.TryConditions(uid, growth.Conditions))
                continue;

            growth.Remaining = TimeSpan.FromSeconds(Math.Max(0, growth.Remaining.TotalSeconds - frameTime));
            if (growth.Remaining == TimeSpan.Zero)
                (ready ??= new()).Add((uid, growth));
        }

        // Spawning/deleting during a query would invalidate its component enumerator.
        if (ready == null)
            return;
        foreach (var animal in ready)
        {
            animal.Comp.RetryRemaining = TimeSpan.FromSeconds(1);
            TryGrow(animal);
        }
    }

    private void TryGrow(Entity<CEAnimalGrowthComponent> animal)
    {
        if (TerminatingOrDeleted(animal) || !TryComp(animal, out TransformComponent? sourceTransform) ||
            sourceTransform.MapUid == null)
            return;

        var rotation = _transform.GetWorldRotation(sourceTransform);
        _containers.TryGetContainingContainer((animal, sourceTransform, null), out var container);
        EntityUid adult;
        try
        {
            adult = Spawn(animal.Comp.ResultPrototype, _transform.GetMapCoordinates(animal, sourceTransform), rotation: rotation);
        }
        catch (Exception exception)
        {
            Log.Warning($"Could not grow {ToPrettyString(animal)}: {exception.Message}");
            return;
        }

        if (!TryTransferState(animal, adult) || container != null && !TryReplaceInContainer(animal, adult, container))
        {
            QueueDel(adult);
            return;
        }

        _transform.SetWorldRotation(adult, rotation);
        if (_mind.TryGetMind(animal, out var mindId, out var mind))
            _mind.TransferTo(mindId, adult, mind: mind);

        QueueDel(animal);
    }

    private bool TryReplaceInContainer(EntityUid source, EntityUid replacement, BaseContainer container)
    {
        if (TerminatingOrDeleted(container.Owner) || !container.Contains(source) ||
            !_containers.TryGetContainer(container.Owner, container.ID, out var current) || current != container)
            return false;

        // ItemSlots owns its filters and locks. A growth replacement must obey those too.
        if (TryComp<ItemSlotsComponent>(container.Owner, out var slots) &&
            _itemSlots.TryGetSlot((container.Owner, slots), container.ID, out var slot) && slot.ContainerSlot == container)
        {
            if (!_itemSlots.CanInsert(container.Owner, slot, replacement, null, swap: true) ||
                !_itemSlots.TryEject(container.Owner, slot, null, out var removed) || removed != source)
                return false;

            if (_itemSlots.TryInsert(container.Owner, slot, replacement, null))
                return true;
        }
        else
        {
            if (_containers.Insert(replacement, container))
                return true;

            if (!_containers.CanInsert(replacement, container, assumeEmpty: true) ||
                !_containers.Remove(source, container, destination: Transform(container.Owner).Coordinates))
                return false;

            if (_containers.Insert(replacement, container))
                return true;
        }

        // Insertion callbacks may delete the host or replace its container after ejecting the source.
        // In that case keep the original animal outside: inserting into a dying owner would delete it.
        if (TerminatingOrDeleted(container.Owner) ||
            !_containers.TryGetContainer(container.Owner, container.ID, out current) || current != container)
            return false;

        // Restore previous ownership only while that container still belongs to the live host.
        if (!_containers.Insert(source, container))
            Log.Error($"Could not restore {ToPrettyString(source)} to '{container.ID}' after failed growth.");
        return false;
    }

    private bool TryTransferState(EntityUid source, EntityUid replacement)
    {
        if (HasComp<DamageableComponent>(source))
        {
            if (!TryComp<DamageableComponent>(replacement, out var damageable) ||
                !_mobThreshold.GetScaledDamage(source, replacement, out var damage) || damage == null)
                return false;
            _damageable.SetDamage((replacement, damageable), damage);
        }

        if (TryComp<SatiationComponent>(source, out var needs))
        {
            if (!TryComp<SatiationComponent>(replacement, out var adultNeeds))
                return false;
            foreach (var type in needs.Satiations.Keys)
            {
                if (!adultNeeds.Has(type) || _satiation.GetValueOrNull((source, needs), type) is not { } value ||
                    !float.IsFinite(value))
                    return false;
                _satiation.SetValue((replacement, adultNeeds), type, value);
            }
        }
        return !TerminatingOrDeleted(source) && !TerminatingOrDeleted(replacement);
    }
}
