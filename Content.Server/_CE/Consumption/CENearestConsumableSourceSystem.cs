using System.Numerics;
using Content.Shared._CE.Consumption;

namespace Content.Server._CE.Consumption;

/// <summary>
/// Finds the nearest eligible provider for a typed consumable-source strategy.
/// Concrete source systems own provider eligibility and consumable lifecycle;
/// callers may add context-specific exclusions without coupling this layer to them.
/// </summary>
public abstract partial class CENearestConsumableSourceSystem<TSource, TProvider>
    : CEConsumableSourceSystem<TSource>
    where TSource : CEConsumableSourceBase<TSource>
    where TProvider : IComponent
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected sealed override bool TryFindProvider(
        EntityUid consumer,
        TSource source,
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

        foreach (var candidate in _lookup.GetEntitiesInRange<TProvider>(
                     origin.Coordinates,
                     range,
                     LookupFlags.Uncontained))
        {
            if (candidate.Owner == consumer ||
                isProviderAllowed != null && !isProviderAllowed(candidate.Owner) ||
                !TryComp(candidate.Owner, out TransformComponent? candidateTransform) ||
                candidateTransform.MapID != origin.MapID ||
                !IsProviderValid(consumer, candidate, source))
                continue;

            var distance = Vector2.DistanceSquared(
                originPosition,
                _transform.GetWorldPosition(candidateTransform));
            if (!float.IsFinite(distance) || provider.IsValid() &&
                (distance > bestDistance ||
                 distance.Equals(bestDistance) && candidate.Owner.Id > provider.Id))
                continue;

            provider = candidate.Owner;
            bestDistance = distance;
        }

        return provider.IsValid();
    }

    protected override bool CanHandleProvider(EntityUid provider, TSource source)
    {
        return HasComp<TProvider>(provider);
    }

    protected abstract bool IsProviderValid(
        EntityUid consumer,
        Entity<TProvider> provider,
        TSource source);
}
