using Content.Shared._CE.Consumption;

namespace Content.Server._CE.Consumption;

/// <summary>
/// Handler base for one concrete consumable-source strategy.
/// </summary>
public abstract partial class CEConsumableSourceSystem<TSource> : EntitySystem
    where TSource : CEConsumableSourceBase<TSource>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEConsumableFindEvent<TSource>>(OnFind);
        SubscribeLocalEvent<CEConsumableCanHandleProviderEvent<TSource>>(OnCanHandleProvider);
        SubscribeLocalEvent<CEConsumableResolveEvent<TSource>>(OnResolve);
        SubscribeLocalEvent<CEConsumableReleaseEvent<TSource>>(OnRelease);
    }

    private void OnFind(ref CEConsumableFindEvent<TSource> ev)
    {
        ev.Success = TryFindProvider(
            ev.Consumer,
            ev.Source,
            ev.Range,
            ev.IsProviderAllowed,
            out ev.Provider);
    }

    private void OnCanHandleProvider(ref CEConsumableCanHandleProviderEvent<TSource> ev)
    {
        ev.CanHandle = CanHandleProvider(ev.Provider, ev.Source);
    }

    private void OnResolve(ref CEConsumableResolveEvent<TSource> ev)
    {
        ev.Success = TryResolveConsumable(
            ev.Consumer,
            ev.Provider,
            ev.Source,
            out ev.Consumable);
    }

    private void OnRelease(ref CEConsumableReleaseEvent<TSource> ev)
    {
        ReleaseConsumable(
            ev.Consumer,
            ev.Provider,
            ev.Consumable,
            ev.Consumed,
            ev.Source);
    }

    protected abstract bool TryFindProvider(
        EntityUid consumer,
        TSource source,
        float range,
        Predicate<EntityUid>? isProviderAllowed,
        out EntityUid provider);

    protected abstract bool CanHandleProvider(
        EntityUid provider,
        TSource source);

    protected abstract bool TryResolveConsumable(
        EntityUid consumer,
        EntityUid provider,
        TSource source,
        out EntityUid consumable);

    protected virtual void ReleaseConsumable(
        EntityUid consumer,
        EntityUid provider,
        EntityUid consumable,
        bool consumed,
        TSource source)
    {
    }
}
