using Content.Shared._CE.MagicVision.Components;
using Robust.Client.Graphics;

namespace Content.Client._CE.MagicEssence;

/// <summary>
/// Owns the lifecycle of <see cref="CEMagicEssenceOverlay"/>. The overlay stays registered for the whole
/// session and only draws while the local player has <see cref="CEMagicVisionComponent"/>.
/// </summary>
public sealed partial class CEMagicEssenceOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private CEMagicEssenceOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new CEMagicEssenceOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
