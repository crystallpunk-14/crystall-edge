using Content.Shared._CE.DayCycle;
using Content.Server._CE.EntitySlots;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.AnimalHusbandry.Lifecycle;

/// <summary>
/// Applies prototype-authored conditional night transformations.
/// </summary>
public sealed partial class CEConditionalNightGrowthSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private CEFixedEntitySlotSystem _fixedSlots = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEStartNightEvent>(OnStartNight);
        SubscribeLocalEvent<DamageableComponent, CEEntityReplacementStateTransferEvent>(OnTransferDamage);
        SubscribeLocalEvent<SatiationComponent, CEEntityReplacementStateTransferEvent>(OnTransferSatiation);
    }

    private void OnStartNight(CEStartNightEvent args)
    {
        if (!Exists(args.MapUid))
            return;

        var transforms = new List<CEConditionalTransform>();
        var query = EntityQueryEnumerator<CEConditionalNightGrowthComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var growth, out var transform))
        {
            if (transform.MapUid != args.MapUid ||
                growth.RequiredSuccessfulNights <= 0 ||
                !_conditions.TryConditions(uid, growth.Conditions))
                continue;

            growth.SuccessfulNights++;
            if (growth.SuccessfulNights < growth.RequiredSuccessfulNights)
                continue;

            transforms.Add(new CEConditionalTransform(uid, growth.ResultPrototype, args.MapUid));
        }

        foreach (var transform in transforms)
            ApplyTransform(transform);
    }

    private void ApplyTransform(CEConditionalTransform transform)
    {
        if (!Exists(transform.Source) ||
            !TryComp(transform.Source, out TransformComponent? sourceTransform) ||
            sourceTransform.MapUid != transform.Map)
            return;

        var mapCoordinates = _transform.GetMapCoordinates(transform.Source, sourceTransform);
        var worldRotation = _transform.GetWorldRotation(sourceTransform);
        var localRotation = sourceTransform.LocalRotation;
        _containers.TryGetContainingContainer((transform.Source, sourceTransform, null), out var containingContainer);

        EntityUid replacement;
        try
        {
            replacement = Spawn(transform.Prototype, mapCoordinates, rotation: worldRotation);
        }
        catch (Exception exception)
        {
            Log.Warning($"CE conditional night growth failed for {ToPrettyString(transform.Source)}: {exception.Message}");
            return;
        }

        if (!TryTransferState(transform.Source, replacement))
        {
            QueueDel(replacement);
            return;
        }

        var replacementTransform = Transform(replacement);
        _transform.SetLocalRotation(replacement, localRotation, replacementTransform);

        var replacedInContainer = true;
        if (_fixedSlots.TryGetSlot(transform.Source, out _, out _))
            replacedInContainer = _fixedSlots.TryReplace(transform.Source, replacement);
        else if (containingContainer != null)
            replacedInContainer = TryReplaceInContainer(
                transform.Source,
                replacement,
                containingContainer);

        if (!replacedInContainer)
        {
            QueueDel(replacement);
            return;
        }

        if (_mind.TryGetMind(transform.Source, out var mindId, out var mind))
            _mind.TransferTo(mindId, replacement, mind: mind);

        QueueDel(transform.Source);
    }

    private bool TryReplaceInContainer(
        EntityUid source,
        EntityUid replacement,
        BaseContainer container)
    {
        if (_containers.Insert(replacement, container))
            return true;

        // A slot container can reject the replacement only because the source still occupies it.
        // Verify the empty-container contract before temporarily removing the source.
        if (!_containers.CanInsert(replacement, container, assumeEmpty: true) ||
            !TryComp(container.Owner, out TransformComponent? ownerTransform) ||
            !_containers.Remove(
                source,
                container,
                destination: ownerTransform.Coordinates))
            return false;

        if (_containers.Insert(replacement, container))
            return true;

        if (!_containers.Insert(source, container))
            Log.Error($"Could not restore {ToPrettyString(source)} to container '{container.ID}' after a failed conditional night transformation in {ToPrettyString(container.Owner)}.");

        return false;
    }

    private bool TryTransferState(EntityUid source, EntityUid replacement)
    {
        var transfer = new CEEntityReplacementStateTransferEvent(replacement);
        RaiseLocalEvent(source, ref transfer);
        return !transfer.Cancelled && Exists(source) && Exists(replacement);
    }

    private void OnTransferDamage(
        Entity<DamageableComponent> source,
        ref CEEntityReplacementStateTransferEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<DamageableComponent>(args.Replacement, out var replacement) ||
            !_mobThreshold.GetScaledDamage(source.Owner, args.Replacement, out var damage) ||
            damage == null)
        {
            args.Cancelled = true;
            return;
        }

        _damageable.SetDamage((args.Replacement, replacement), damage);
    }

    private void OnTransferSatiation(
        Entity<SatiationComponent> source,
        ref CEEntityReplacementStateTransferEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<SatiationComponent>(args.Replacement, out var replacement))
        {
            args.Cancelled = true;
            return;
        }

        foreach (var type in source.Comp.Satiations.Keys)
        {
            if (!replacement.Has(type) || _satiation.GetValueOrNull(source, type) is not { } value ||
                !float.IsFinite(value))
            {
                args.Cancelled = true;
                return;
            }

            _satiation.SetValue((args.Replacement, replacement), type, value);
        }
    }

    private readonly record struct CEConditionalTransform(
        EntityUid Source,
        EntProtoId Prototype,
        EntityUid Map);
}
