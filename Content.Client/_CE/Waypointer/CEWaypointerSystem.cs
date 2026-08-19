using Content.Shared.CombatMode;
using Content.Shared._CE.Waypointer;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Shared.Player;

namespace Content.Client._CE.Waypointer;

/// <summary>
/// The client-side system handles initializing the overlay, as well as removing and adding it depending on game actions.
/// </summary>
public sealed partial class CEWaypointerSystem : CESharedWaypointerSystem
{
    [Dependency] private IPlayerManager  _player = default!;
    [Dependency] private IClientGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;

    private CEWaypointerOverlay _waypointerOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _waypointerOverlay = new CEWaypointerOverlay();

        SubscribeLocalEvent<CEWaypointerComponent, ToggleCombatActionEvent>(OnCombatToggle);

        SubscribeLocalEvent<CEWaypointerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CEWaypointerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    protected override void OnAddition(Entity<CEWaypointerComponent> player, ref ComponentInit args)
    {
        base.OnAddition(player, ref args);

        if (_player.LocalEntity == null || player.Owner != _player.LocalEntity.Value)
            return;

        _overlay.AddOverlay(_waypointerOverlay);
    }

    protected override void OnRemoval(Entity<CEWaypointerComponent> player, ref ComponentRemove args)
    {
        base.OnRemoval(player, ref args);

        if (_player.LocalEntity == null || player.Owner != _player.LocalEntity.Value)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay);
    }

    private void OnCombatToggle(Entity<CEWaypointerComponent> combatant, ref ToggleCombatActionEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // Somehow, args.Toggle does not change from false to true whenever. So we are using this.
        // When combat mode is on, turn off the overlay, so it's less distraction.
        if (args.Action.Comp.Toggled)
            _overlay.AddOverlay(_waypointerOverlay);
        else
            _overlay.RemoveOverlay(_waypointerOverlay);
    }

    private void OnPlayerAttached(Entity<CEWaypointerComponent> mob, ref LocalPlayerAttachedEvent args)
    {
        if (args.Entity != _player.LocalEntity)
            return;

        _overlay.AddOverlay(_waypointerOverlay);
    }

    private void OnPlayerDetached(Entity<CEWaypointerComponent> mob, ref LocalPlayerDetachedEvent args)
    {
        if (args.Entity != _player.LocalEntity)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay);
    }
}
