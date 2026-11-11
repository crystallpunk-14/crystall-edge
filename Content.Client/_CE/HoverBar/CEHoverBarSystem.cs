using Robust.Client.Graphics;

namespace Content.Client._CE.HoverBar;

/// <summary>
/// Owns <see cref="CEHoverBarOverlay"/>: it exists only while at least one ability HUD of the local player is active
/// </summary>
public sealed partial class CEHoverBarSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;

    private readonly HashSet<Type> _activeSources = new();
    private CEHoverBarOverlay? _overlay;

    public override void Shutdown()
    {
        base.Shutdown();

        _activeSources.Clear();
        RemoveOverlay();
    }

    public void SetSourceActive(EntitySystem source, bool active)
    {
        if (active)
            _activeSources.Add(source.GetType());
        else
            _activeSources.Remove(source.GetType());

        if (_activeSources.Count > 0 && _overlay == null)
        {
            _overlay = new CEHoverBarOverlay();
            _overlayManager.AddOverlay(_overlay);
        }
        else if (_activeSources.Count == 0)
        {
            RemoveOverlay();
        }
    }

    private void RemoveOverlay()
    {
        if (_overlay == null)
            return;

        _overlayManager.RemoveOverlay(_overlay);
        _overlay = null;
    }
}
