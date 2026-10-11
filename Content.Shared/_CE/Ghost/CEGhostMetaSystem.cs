using Content.Shared._CE.Roles;
using Content.Shared.Actions;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Robust.Shared.Network;

namespace Content.Shared._CE.Ghost;

/// <summary>
/// Decides whether a ghost may see meta information (secret roles, objectives) and lets ghosts that can
/// still return to their body give that return up to gain it.
/// </summary>
public sealed partial class CEGhostMetaSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private CESecretRoleIconSystem _secretRoleIcon = default!;

    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;
    [Dependency] private EntityQuery<CEGhostMetaComponent> _metaQuery = default!;

    /// <summary>
    /// Whether the viewer is a ghost allowed to see meta information: admin ghosts always, others once they can't return.
    /// </summary>
    public bool CanSeeMeta(EntityUid viewer)
    {
        return _ghostQuery.TryComp(viewer, out var ghost) && (ghost.CanGhostInteract || !ghost.CanReturnToBody);
    }

    [SubscribeLocalEvent]
    private void OnCanReturnToBodyChanged(Entity<GhostComponent> ent, ref CEGhostCanReturnToBodyChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (_metaQuery.TryComp(ent, out var meta))
        {
            if (CanSeeMeta(ent))
                _actions.RemoveAction(ent.Owner, meta.ActionEntity);
            else
                _actions.AddAction(ent, ref meta.ActionEntity, meta.Action);

            Dirty(ent, meta);
        }

        _secretRoleIcon.DirtyAllRoleIcons();

        var ev = new CEGhostMetaAccessChangedEvent(ent);
        RaiseLocalEvent(ref ev);
    }

    [SubscribeLocalEvent]
    private void OnForfeitReturn(Entity<CEGhostMetaComponent> ent, ref CEGhostForfeitReturnActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        // Taking full ownership of the ghost leaves the body without a mind and turns off returning to it.
        if (_mind.TryGetMind(ent, out var mindId, out var mind))
            _mind.TransferTo(mindId, ent, mind: mind);
    }
}

/// <summary>
/// Raised on a ghost when <see cref="GhostComponent.CanReturnToBody"/> changes.
/// </summary>
[ByRefEvent]
public record struct CEGhostCanReturnToBodyChangedEvent;

/// <summary>
/// Broadcast after a ghost gained or lost access to meta information.
/// </summary>
[ByRefEvent]
public record struct CEGhostMetaAccessChangedEvent(EntityUid Ghost);

public sealed partial class CEGhostForfeitReturnActionEvent : InstantActionEvent;
