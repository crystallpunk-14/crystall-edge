using System.Linq;
using Content.Shared._CE.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Objectives;

/// <summary>
/// Base class for CE objective condition systems that need common behavior - specifically relay
/// components that must exist on whatever body a holder's mind currently controls, kept in sync as
/// the mind swaps bodies. Extend this instead of copy-pasting the mind-added/removed handling.
/// </summary>
public abstract partial class CEBaseObjectiveSystem<TComponent> : EntitySystem
    where TComponent : Component
{
    [Dependency] protected SharedMindSystem MindSys = default!;
    [Dependency] protected CESharedObjectiveSystem ObjectivesSys = default!;

    /// <summary>
    /// Relay components this objective relies on existing on the holder's current body. These are
    /// automatically added/kept as the mind moves between bodies. Relays should be stateless -
    /// they're transient and not meant to carry data of their own.
    /// </summary>
    public virtual Type[] RelayComponents => Array.Empty<Type>();

    public override void Initialize()
    {
        DebugTools.Assert(
            RelayComponents.All(x => x.IsAssignableTo(typeof(IComponent))),
            $"One or more relay components on {GetType()} aren't actual components. Check for typos.");

        SubscribeLocalEvent<TComponent, MindGotAddedEvent>(OnMindGotAdded);
        SubscribeLocalEvent<TComponent, MindGotRemovedEvent>(OnMindGotRemoved);
        SubscribeLocalEvent<TComponent, CEGetObjectiveProgressEvent>(GetObjectiveProgress);
        SubscribeLocalEvent<TComponent, CEInitializeObjectiveEvent>(InitializeObjective);
    }

    /// <summary>
    /// Resolves progress on the objective. Mandatory override (even if a no-op) so it isn't
    /// forgotten - defaults to leaving progress untouched.
    /// </summary>
    protected virtual void GetObjectiveProgress(Entity<TComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
    }

    /// <summary>
    /// Called right after the objective is spawned and added to its holder, to set up title/target/etc.
    /// </summary>
    protected virtual void InitializeObjective(Entity<TComponent> ent, ref CEInitializeObjectiveEvent args)
    {
    }

    /// <summary>
    /// Ensures relay components on the mind's current body. Override should call base first.
    /// </summary>
    [MustCallBase]
    protected virtual void OnMindGotAdded(Entity<TComponent> ent, ref MindGotAddedEvent args)
    {
        EnsureRelaysOnMind(args.Mind.AsNullable());
    }

    /// <summary>
    /// Override should call base first. Relays on the old body are left alone - they're inert once
    /// nothing is tracking a mind for that body anymore, and get cleaned up naturally when the body
    /// itself is deleted.
    /// </summary>
    [MustCallBase]
    protected virtual void OnMindGotRemoved(Entity<TComponent> ent, ref MindGotRemovedEvent args)
    {
    }

    private void EnsureRelaysOnMind(Entity<MindComponent?> mind)
    {
        // Not everything holding objectives is a mind.
        if (!Resolve(mind, ref mind.Comp, false))
            return;

        if (mind.Comp.CurrentEntity is not { } body)
            return;

        foreach (var relayType in RelayComponents)
        {
            if (!HasComp(body, relayType))
                AddComp(body, Factory.GetComponent(relayType));
        }
    }
}
