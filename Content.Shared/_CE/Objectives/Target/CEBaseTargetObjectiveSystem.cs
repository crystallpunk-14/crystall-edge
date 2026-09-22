using System.Linq;
using Content.Shared._CE.Objectives.Target.Components;

namespace Content.Shared._CE.Objectives.Target;

/// <summary>
/// Variant of <see cref="CEBaseObjectiveSystem{TComponent}"/> for objectives built on top of
/// <see cref="CETargetObjectiveComponent"/>. Automatically adds/removes <see cref="TargetRelayComponents"/>
/// on the objective's target as it changes, so condition logic doesn't need its own marker-management
/// boilerplate for every target-based objective.
/// </summary>
public abstract partial class CEBaseTargetObjectiveSystem<TComponent> : CEBaseObjectiveSystem<TComponent>
    where TComponent : Component
{
    [Dependency] protected CETargetObjectiveSystem TargetObjective = default!;

    /// <summary>
    /// Relay components added to the target while it's being targeted by this objective type -
    /// e.g. a marker component so target-side event subscriptions (mob state changes, etc.) know
    /// which objectives to refresh.
    /// </summary>
    public virtual Type[] TargetRelayComponents => Array.Empty<Type>();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TComponent, CEObjectiveTargetChangedEvent>(OnTargetChanged);
    }

    [MustCallBase]
    protected virtual void OnTargetChanged(Entity<TComponent> ent, ref CEObjectiveTargetChangedEvent args)
    {
        // The old target might still be targeted by another objective of this same type (e.g. two
        // different players both have a "protect Bob" objective) - only strip the relay once nothing
        // of this type targets it anymore. SetTarget already removes `ent` from the marker's
        // Objectives set before raising this event, so GetTargetingObjectives here reflects that.
        if (args.OldTarget is { } oldTarget &&
            !TerminatingOrDeleted(oldTarget) &&
            !GetTargetingObjectives(oldTarget).Any())
        {
            foreach (var relayType in TargetRelayComponents)
            {
                // Use the Type overload (not Factory.GetComponent(relayType)) - that would create a
                // throwaway unattached instance and hand it to RemComp(uid, IComponent), which removes
                // by that exact reference instead of looking up the real attached one.
                RemComp(oldTarget, relayType);
            }
        }

        if (args.NewTarget is { } newTarget)
        {
            foreach (var relayType in TargetRelayComponents)
            {
                if (!HasComp(newTarget, relayType))
                    AddComp(newTarget, Factory.GetComponent(relayType));
            }
        }
    }

    /// <summary>
    /// Calls <see cref="CESharedObjectiveSystem.RefreshObjectiveProgress"/> on every objective of
    /// type <typeparamref name="TComponent"/> targeting the given entity.
    /// </summary>
    protected void RefreshTargetingObjectives(EntityUid target)
    {
        foreach (var objective in GetTargetingObjectives(target))
            ObjectivesSys.RefreshObjectiveProgress(objective.Owner);
    }

    /// <summary>
    /// Helper version of <see cref="CETargetObjectiveSystem.GetTargetingObjectives{TComponent}"/>.
    /// </summary>
    protected IEnumerable<Entity<TComponent>> GetTargetingObjectives(EntityUid target)
    {
        return TargetObjective.GetTargetingObjectives<TComponent>(target);
    }
}
