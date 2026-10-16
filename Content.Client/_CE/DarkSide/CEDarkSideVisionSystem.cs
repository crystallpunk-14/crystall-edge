using System.Linq;
using Content.Shared._CE.DarkSide;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._CE.DarkSide;

/// <summary>
/// Shows <see cref="CEDarkSideVisionOverlay"/> while the local player has a
/// <see cref="CEDarkSideVisionStatusEffectComponent"/> status effect.
/// </summary>
public sealed partial class CEDarkSideVisionSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private CEDarkSideVisionOverlay? _overlay;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<CEDarkSideVisionStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (args.Target == _player.LocalEntity)
            AddOverlay();
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CEDarkSideVisionStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (args.Target != _player.LocalEntity)
            return;

        // Already out of the status effect container here, so this only sees the other active effects.
        if (_statusEffects.EnumerateStatusEffects<CEDarkSideVisionStatusEffectComponent>(args.Target).Any())
            return;

        // Fade out instead of cutting off; the overlay is removed once it has faded out completely.
        if (_overlay != null)
            _overlay.Active = false;
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<CEDarkSideVisionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        AddOverlay();
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<CEDarkSideVisionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        RemoveOverlay();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_overlay is { FadedOut: true })
            RemoveOverlay();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        RemoveOverlay();
    }

    private void AddOverlay()
    {
        // Re-entering while still fading out just turns the fade back around.
        if (_overlay != null)
        {
            _overlay.Active = true;
            return;
        }

        _overlay = new CEDarkSideVisionOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    private void RemoveOverlay()
    {
        if (_overlay == null)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Dispose();
        _overlay = null;
    }
}
