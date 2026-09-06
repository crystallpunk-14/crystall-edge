using JetBrains.Annotations;

namespace Content.Shared._CE.Consumption;

/// <summary>
/// Prototype-authored strategy for locating a provider and resolving the physical
/// entity consumed from it. Concrete provider policy lives in typed systems.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEConsumableSource
{
    public bool TryFindProvider(
        EntityUid consumer,
        float range,
        IEntityManager entMan,
        out EntityUid provider)
    {
        return TryFindProvider(consumer, range, null, entMan, out provider);
    }

    /// <summary>
    /// Locates a provider while allowing the caller to supply a context-specific
    /// eligibility predicate. The source remains unaware of GOAP or retry policy.
    /// </summary>
    public abstract bool TryFindProvider(
        EntityUid consumer,
        float range,
        Predicate<EntityUid>? isProviderAllowed,
        IEntityManager entMan,
        out EntityUid provider);

    /// <summary>
    /// Returns whether this strategy owns the provider category. This is a
    /// routing check only; it does not promise that the provider is currently
    /// consumable.
    /// </summary>
    public abstract bool CanHandleProvider(
        EntityUid provider,
        IEntityManager entMan);

    public abstract bool TryResolveConsumable(
        EntityUid consumer,
        EntityUid provider,
        IEntityManager entMan,
        out EntityUid consumable);

    /// <summary>
    /// Releases a previously resolved consumable to its provider-specific lifecycle.
    /// The canonical ingestion system remains responsible for committing consumption.
    /// Implementations must ignore tuples they do not own so a composite can
    /// clean up safely even after the provider is deleted or changes category.
    /// </summary>
    public abstract void ReleaseConsumable(
        EntityUid consumer,
        EntityUid provider,
        EntityUid consumable,
        bool consumed,
        IEntityManager entMan);
}

/// <summary>
/// Type-safe event dispatch for one consumable-source strategy.
/// </summary>
public abstract partial class CEConsumableSourceBase<TSource> : CEConsumableSource
    where TSource : CEConsumableSourceBase<TSource>
{
    public override bool TryFindProvider(
        EntityUid consumer,
        float range,
        Predicate<EntityUid>? isProviderAllowed,
        IEntityManager entMan,
        out EntityUid provider)
    {
        provider = default;
        if (this is not TSource self)
            return false;

        var ev = new CEConsumableFindEvent<TSource>(self, consumer, range, isProviderAllowed);
        entMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
        provider = ev.Provider;
        return ev.Success;
    }

    public override bool CanHandleProvider(
        EntityUid provider,
        IEntityManager entMan)
    {
        if (this is not TSource self)
            return false;

        var ev = new CEConsumableCanHandleProviderEvent<TSource>(self, provider);
        entMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
        return ev.CanHandle;
    }

    public override bool TryResolveConsumable(
        EntityUid consumer,
        EntityUid provider,
        IEntityManager entMan,
        out EntityUid consumable)
    {
        consumable = default;
        if (this is not TSource self)
            return false;

        var ev = new CEConsumableResolveEvent<TSource>(self, consumer, provider);
        entMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
        consumable = ev.Consumable;
        return ev.Success;
    }

    public override void ReleaseConsumable(
        EntityUid consumer,
        EntityUid provider,
        EntityUid consumable,
        bool consumed,
        IEntityManager entMan)
    {
        if (this is not TSource self)
            return;

        var ev = new CEConsumableReleaseEvent<TSource>(
            self,
            consumer,
            provider,
            consumable,
            consumed);
        entMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
    }
}

[ByRefEvent]
public record struct CEConsumableFindEvent<TSource>(
    TSource Source,
    EntityUid Consumer,
    float Range,
    Predicate<EntityUid>? IsProviderAllowed)
    where TSource : CEConsumableSourceBase<TSource>
{
    public bool Success;
    public EntityUid Provider;
}

[ByRefEvent]
public record struct CEConsumableCanHandleProviderEvent<TSource>(
    TSource Source,
    EntityUid Provider)
    where TSource : CEConsumableSourceBase<TSource>
{
    public bool CanHandle;
}

[ByRefEvent]
public record struct CEConsumableResolveEvent<TSource>(
    TSource Source,
    EntityUid Consumer,
    EntityUid Provider)
    where TSource : CEConsumableSourceBase<TSource>
{
    public bool Success;
    public EntityUid Consumable;
}

[ByRefEvent]
public readonly record struct CEConsumableReleaseEvent<TSource>(
    TSource Source,
    EntityUid Consumer,
    EntityUid Provider,
    EntityUid Consumable,
    bool Consumed)
    where TSource : CEConsumableSourceBase<TSource>;
