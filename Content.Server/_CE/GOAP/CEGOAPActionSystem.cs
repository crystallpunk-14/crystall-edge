using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Robust.Shared.Map;

namespace Content.Server._CE.GOAP;

/// <summary>
/// Base EntitySystem for handling GOAP action events.
/// Concrete action systems inherit from this and implement the action logic.
/// </summary>
public abstract partial class CEGOAPActionSystem<T> : EntitySystem where T : CEGOAPActionBase<T>
{
    [Dependency] protected CEGOAPSystem Goap = default!;
    [Dependency] private EntityQuery<TransformComponent> _coordsXformQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGOAPComponent, CEGOAPActionInitEvent<T>>(OnActionInit);
        SubscribeLocalEvent<CEGOAPComponent, CEGOAPActionCanExecuteEvent<T>>(OnCanExecute);
        SubscribeLocalEvent<CEGOAPComponent, CEGOAPActionStartupEvent<T>>(OnActionStartup);
        SubscribeLocalEvent<CEGOAPComponent, CEGOAPActionUpdateEvent<T>>(OnActionUpdate);
        SubscribeLocalEvent<CEGOAPComponent, CEGOAPActionShutdownEvent<T>>(OnActionShutdown);
    }

    /// <summary>
    /// Called once during entity map initialization. Override to perform one-time setup.
    /// </summary>
    protected virtual void OnActionInit(Entity<CEGOAPComponent> ent, ref CEGOAPActionInitEvent<T> args)
    {
    }

    /// <summary>
    /// Called during planning to check if this action can be executed.
    /// Override to add feasibility checks (e.g., cooldowns, resource availability).
    /// Set args.CanExecute = false to exclude this action from the current planning cycle.
    /// </summary>
    protected virtual void OnCanExecute(Entity<CEGOAPComponent> ent, ref CEGOAPActionCanExecuteEvent<T> args)
    {
    }

    /// <summary>
    /// Called once when the action has just started to be performed.
    /// </summary>
    protected virtual void OnActionStartup(Entity<CEGOAPComponent> ent, ref CEGOAPActionStartupEvent<T> args)
    {
    }

    /// <summary>
    /// Called each tick while the action is being performed. The action should update the status of the action in args.Status.
    /// </summary>
    protected virtual void OnActionUpdate(Entity<CEGOAPComponent> ent, ref CEGOAPActionUpdateEvent<T> args)
    {
    }

    /// <summary>
    /// Called once when the action has finished performing, regardless of whether it succeeded, failed, or was interrupted.
    /// </summary>
    protected virtual void OnActionShutdown(Entity<CEGOAPComponent> ent, ref CEGOAPActionShutdownEvent<T> args)
    {
    }

    /// <summary>
    /// Resolves a <see cref="CEGOAPTargetSelector"/> to world coordinates.
    /// Prefers the resolved entity's transform, falling back to a raw position.
    /// </summary>
    protected bool TryResolveCoords(EntityUid agent, CEGOAPTargetSelector? selector, out EntityCoordinates coords)
    {
        coords = default;
        if (selector == null)
            return false;

        var result = selector.Resolve(agent, EntityManager);
        if (result.Entity is { } e && _coordsXformQuery.TryGetComponent(e, out var xform))
        {
            coords = xform.Coordinates;
            return true;
        }

        if (result.Position is { } p)
        {
            coords = p;
            return true;
        }

        return false;
    }
}
