/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.ZLevels.Core.Components;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;

namespace Content.Shared._CE.ZLevels.Core.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    private readonly List<EntityUid> _activeBodies = new();

    public IReadOnlyList<EntityUid> ActiveBodies => _activeBodies;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEZPhysicsComponent> entity, ref MapInitEvent args)
    {
        RefreshBody(entity);
        SyncCurrentZLevel(entity);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CEZPhysicsComponent> entity, ref ComponentShutdown args)
    {
        SleepBody((entity, entity));
    }

    [SubscribeLocalEvent]
    private void OnAnchorStateChanged(Entity<CEZPhysicsComponent> entity, ref AnchorStateChangedEvent args)
    {
        RefreshBody(entity);
    }

    [SubscribeLocalEvent]
    private void OnPhysicsBodyTypeChanged(Entity<CEZPhysicsComponent> entity, ref PhysicsBodyTypeChangedEvent args)
    {
        RefreshBody(entity);
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<CEZPhysicsComponent> entity, ref EntParentChangedMessage args)
    {
        RefreshBody(entity);
        SyncCurrentZLevel(entity);

        if (ZPhysicsQuery.TryComp(args.OldParent, out var oldParentPhysics))
        {
            SetZPosition((entity, entity), oldParentPhysics.LocalPosition);
            return;
        }

        var xform = Transform(entity);
        if (xform.ParentUid != xform.GridUid)
            ResetZState(entity);
    }

    /// <summary>
    /// Clears height and velocity without waking the body: nested bodies are moved by their parent.
    /// </summary>
    private void ResetZState(Entity<CEZPhysicsComponent> entity)
    {
        if (entity.Comp.LocalPosition != 0)
        {
            entity.Comp.LocalPosition = 0;
            DirtyField(entity, entity.Comp, nameof(CEZPhysicsComponent.LocalPosition));
        }

        if (entity.Comp.Velocity != 0)
        {
            entity.Comp.Velocity = 0;
            DirtyField(entity, entity.Comp, nameof(CEZPhysicsComponent.Velocity));
        }
    }

    /// <summary>
    /// Only unanchored, non-static bodies parented directly to the grid simulate z-physics.
    /// </summary>
    private bool CanBeActive(EntityUid uid, TransformComponent xform)
    {
        if (xform.ParentUid != xform.GridUid || xform.Anchored)
            return false;

        return !_physicsQuery.TryComp(uid, out var physics) || physics.BodyType != BodyType.Static;
    }

    private void SyncCurrentZLevel(Entity<CEZPhysicsComponent> entity)
    {
        if (!_zMapQuery.TryComp(Transform(entity).MapUid, out var zLevel))
            return;

        if (entity.Comp.CurrentZLevel == zLevel.Depth)
            return;

        entity.Comp.CurrentZLevel = zLevel.Depth;
        DirtyField(entity, entity.Comp, nameof(CEZPhysicsComponent.CurrentZLevel));
    }

    [PublicAPI]
    public void WakeBody(Entity<CEZPhysicsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (_activeBodies.Contains(entity))
            return;

        if (!_transformQuery.TryComp(entity, out var xform) || !CanBeActive(entity, xform))
            return;

        entity.Comp.Sleeping = false;
        entity.Comp.SleepTimer = 0f;

        _activeBodies.Add(entity);
    }

    [PublicAPI]
    public void SleepBody(Entity<CEZPhysicsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        entity.Comp.Sleeping = true;
        entity.Comp.SleepTimer = 0f;

        _activeBodies.Remove(entity);
    }

    [PublicAPI]
    public void RefreshBody(Entity<CEZPhysicsComponent> entity)
    {
        if (TerminatingOrDeleted(entity))
        {
            SleepBody((entity, entity));
            return;
        }

        if (!CanBeActive(entity, Transform(entity)))
        {
            SleepBody((entity, entity));
            return;
        }

        WakeBody((entity, entity));
    }
}
