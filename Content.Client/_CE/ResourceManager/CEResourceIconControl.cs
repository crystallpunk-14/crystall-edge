using Content.Shared._CE.ResourceManager;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._CE.ResourceManager;

/// <summary>
/// Shows the layered icon of a resource requirement, stretched to the control size.
/// </summary>
public sealed class CEResourceIconControl : Control
{
    private readonly CEResourceIconRenderer _renderer = new();
    private List<CEResourceIconLayer> _layers = new();

    public void SetLayers(List<CEResourceIconLayer> layers)
    {
        _layers = layers;

        if (IsInsideTree)
            _renderer.SetLayers(_layers);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _renderer.SetLayers(_layers);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _renderer.Clear();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        _renderer.Draw(handle, PixelSizeBox);
    }
}
