using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared._CE.GOAP.Selectors;

/// <summary>
/// Data-only base for polymorphic GOAP target selectors. A selector resolves a target
/// to an entity, a coordinate, or both. Logic is implemented by per-type EntitySystems
/// derived from <see cref="CEGOAPTargetSelectorSystem{TSelector}"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEGOAPTargetSelector
{
    /// <summary>
    /// Resolves the selector to an entity and/or coordinate.
    /// </summary>
    public abstract CEGOAPSelectorResult Resolve(EntityUid agent, IEntityManager entMan);
}

/// <summary>
/// Generic base providing automatic event dispatch for concrete selector types.
/// </summary>
public abstract partial class CEGOAPTargetSelectorBase<T> : CEGOAPTargetSelector
    where T : CEGOAPTargetSelectorBase<T>
{
    public override CEGOAPSelectorResult Resolve(EntityUid agent, IEntityManager entMan)
    {
        if (this is not T self)
            return default;

        var ev = new CEGOAPSelectorResolveEvent<T>(self, agent);
        entMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
        return new CEGOAPSelectorResult(ev.Entity, ev.Position);
    }
}

/// <summary>
/// Result of a selector resolution. Either or both fields may be set.
/// </summary>
public readonly record struct CEGOAPSelectorResult(EntityUid? Entity, EntityCoordinates? Position)
{
    public bool HasResult => Entity != null || Position != null;
}

/// <summary>
/// Broadcast event raised when a GOAP selector is being resolved.
/// The handling system sets <see cref="Entity"/> and/or <see cref="Position"/>.
/// </summary>
[ByRefEvent]
public record struct CEGOAPSelectorResolveEvent<T>(T Selector, EntityUid Agent)
    where T : CEGOAPTargetSelectorBase<T>
{
    public EntityUid? Entity;
    public EntityCoordinates? Position;
}

/// <summary>
/// Base system that auto-subscribes to <see cref="CEGOAPSelectorResolveEvent{TSelector}"/>.
/// </summary>
public abstract partial class CEGOAPTargetSelectorSystem<TSelector> : EntitySystem
    where TSelector : CEGOAPTargetSelectorBase<TSelector>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEGOAPSelectorResolveEvent<TSelector>>(OnResolve);
    }

    private void OnResolve(ref CEGOAPSelectorResolveEvent<TSelector> ev)
    {
        Resolve(ref ev);
    }

    protected abstract void Resolve(ref CEGOAPSelectorResolveEvent<TSelector> ev);
}
