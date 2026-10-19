using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions;

public sealed partial class CEToggleStatusEffectsActionSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    [SubscribeLocalEvent]
    private void OnToggle(Entity<TransformComponent> ent, ref CEToggleStatusEffectsActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.Toggle = true;

        SetState(args.Performer, args, !args.Action.Comp.Toggled);
    }

    [SubscribeLocalEvent]
    private void OnGranted(Entity<ActionComponent> ent, ref CEActionGrantedEvent args)
    {
        if (_net.IsClient || _actions.GetEvent(ent) is not CEToggleStatusEffectsActionEvent toggle)
            return;

        SetState(args.Performer, toggle, ent.Comp.Toggled);
    }

    [SubscribeLocalEvent]
    private void OnRevoked(Entity<ActionComponent> ent, ref CEActionRevokedEvent args)
    {
        if (_net.IsClient || TerminatingOrDeleted(args.Performer))
            return;

        if (_actions.GetEvent(ent) is not CEToggleStatusEffectsActionEvent toggle)
            return;

        if (toggle.Disabled is { } disabled)
            _statusEffects.TryRemoveStatusEffect(args.Performer, disabled);

        if (toggle.Enabled is { } enabled)
            _statusEffects.TryRemoveStatusEffect(args.Performer, enabled);
    }

    private void SetState(EntityUid performer, CEToggleStatusEffectsActionEvent toggle, bool enabled)
    {
        var remove = enabled ? toggle.Disabled : toggle.Enabled;
        var apply = enabled ? toggle.Enabled : toggle.Disabled;

        if (remove is { } removeProto)
            _statusEffects.TryRemoveStatusEffect(performer, removeProto);

        if (apply is { } applyProto)
            _statusEffects.TrySetStatusEffectDuration(performer, applyProto);
    }
}

/// <summary>
/// Instant action that switches its performer between two status effects. The status matching the action's
/// toggled state is kept on whoever the action is granted to, and both are removed once it is revoked.
/// </summary>
public sealed partial class CEToggleStatusEffectsActionEvent : InstantActionEvent
{
    /// <summary>
    /// Status effect kept on the performer while the action is not toggled. Null for no status.
    /// </summary>
    [DataField]
    public EntProtoId? Disabled;

    /// <summary>
    /// Status effect kept on the performer while the action is toggled. Null for no status.
    /// </summary>
    [DataField]
    public EntProtoId? Enabled;
}
