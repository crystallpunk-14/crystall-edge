using Content.Shared._CE.Objectives;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;

namespace Content.Server._CE.Objectives;

/// <inheritdoc/>
public sealed partial class CEObjectiveSystem : CESharedObjectiveSystem
{
    [Dependency] private SharedMindSystem _mind = default!;

    // Objectives already held by a mind aren't re-overridden by the engine on reconnect (that
    // only happens for newly-added ones), so re-apply the override to the mind's objectives
    // whenever a session (re)attaches to the body it controls.
    [SubscribeLocalEvent]
    private void OnPlayerAttached(EntityUid uid, MindContainerComponent comp, PlayerAttachedEvent args)
    {
        if (_mind.TryGetMind(uid, out var mindId, out _))
            RefreshHolderOverrides(mindId, args.Player, add: true);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(EntityUid uid, MindContainerComponent comp, PlayerDetachedEvent args)
    {
        if (_mind.TryGetMind(uid, out var mindId, out _))
            RefreshHolderOverrides(mindId, args.Player, add: false);
    }
}
