using System.Numerics;
using Content.Shared._CE.Consumption;

namespace Content.Server._CE.Consumption;

/// <summary>
/// Chooses among leaf source strategies without knowing their provider policy.
/// </summary>
public sealed partial class CECompositeConsumableSourceSystem
    : CEConsumableSourceSystem<CECompositeConsumableSource>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override bool TryFindProvider(
        EntityUid consumer,
        CECompositeConsumableSource source,
        float range,
        Predicate<EntityUid>? isProviderAllowed,
        out EntityUid provider)
    {
        provider = default;
        if (!float.IsFinite(range) || range < 0f ||
            !TryComp(consumer, out TransformComponent? origin))
            return false;

        var originPosition = _transform.GetWorldPosition(origin);
        var bestDistance = float.MaxValue;

        for (var priority = 0; priority < source.Sources.Count; priority++)
        {
            var leaf = source.Sources[priority];
            var leafPriority = priority;
            bool IsAllowed(EntityUid candidate)
            {
                return (isProviderAllowed == null || isProviderAllowed(candidate)) &&
                    IsOwnedBy(source, leafPriority, candidate);
            }

            if (leaf == null ||
                !leaf.TryFindProvider(consumer, range, IsAllowed, EntityManager, out var candidate) ||
                !TryComp(candidate, out TransformComponent? candidateTransform) ||
                candidateTransform.MapID != origin.MapID)
                continue;

            var distance = Vector2.DistanceSquared(
                originPosition,
                _transform.GetWorldPosition(candidateTransform));
            // Earlier authored sources win equal distances; each leaf resolves its own ties.
            if (!float.IsFinite(distance) || provider.IsValid() && distance >= bestDistance)
                continue;

            provider = candidate;
            bestDistance = distance;
        }

        return provider.IsValid();
    }

    protected override bool CanHandleProvider(
        EntityUid provider,
        CECompositeConsumableSource source)
    {
        foreach (var leaf in source.Sources)
        {
            if (leaf != null && leaf.CanHandleProvider(provider, EntityManager))
                return true;
        }

        return false;
    }

    protected override bool TryResolveConsumable(
        EntityUid consumer,
        EntityUid provider,
        CECompositeConsumableSource source,
        out EntityUid consumable)
    {
        consumable = default;
        foreach (var leaf in source.Sources)
        {
            if (leaf == null || !leaf.CanHandleProvider(provider, EntityManager))
                continue;

            return leaf.TryResolveConsumable(
                consumer,
                provider,
                EntityManager,
                out consumable);
        }

        return false;
    }

    protected override void ReleaseConsumable(
        EntityUid consumer,
        EntityUid provider,
        EntityUid consumable,
        bool consumed,
        CECompositeConsumableSource source)
    {
        foreach (var leaf in source.Sources)
        {
            if (leaf == null)
                continue;

            leaf.ReleaseConsumable(
                consumer,
                provider,
                consumable,
                consumed,
                EntityManager);
        }
    }

    private bool IsOwnedBy(CECompositeConsumableSource source, int priority, EntityUid provider)
    {
        for (var index = 0; index <= priority; index++)
        {
            var leaf = source.Sources[index];
            if (leaf == null || !leaf.CanHandleProvider(provider, EntityManager))
                continue;

            return index == priority;
        }

        return false;
    }
}
