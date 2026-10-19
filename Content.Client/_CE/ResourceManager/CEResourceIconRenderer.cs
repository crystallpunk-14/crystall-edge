using System.Numerics;
using Content.Shared._CE.ResourceManager;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client._CE.ResourceManager;

/// <summary>
/// Draws resource icon layers into a screen-space box. Entity layers are drawn from client-side
/// nullspace dummies owned by the renderer; call <see cref="Clear"/> to delete them.
/// </summary>
public sealed partial class CEResourceIconRenderer
{
    [Dependency] private IEntityManager _entMan = default!;

    private readonly SpriteSystem _sprite;
    private readonly List<RenderLayer> _layers = new();

    private readonly record struct RenderLayer(CEResourceIconLayer Layer, Texture? Texture, Entity<SpriteComponent>? Dummy);

    public CEResourceIconRenderer()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entMan.System<SpriteSystem>();
    }

    public bool IsEmpty => _layers.Count == 0;

    public void SetLayers(IEnumerable<CEResourceIconLayer> layers)
    {
        Clear();

        foreach (var layer in layers)
        {
            if (layer.Sprite is { } sprite)
            {
                _layers.Add(new RenderLayer(layer, _sprite.Frame0(sprite), null));
                continue;
            }

            if (layer.Entity is not { } proto)
                continue;

            var dummy = _entMan.SpawnEntity(proto, MapCoordinates.Nullspace);
            if (!_entMan.TryGetComponent<SpriteComponent>(dummy, out var spriteComp))
            {
                _entMan.DeleteEntity(dummy);
                continue;
            }

            _sprite.SetColor((dummy, spriteComp), layer.Color);
            _layers.Add(new RenderLayer(layer, null, (dummy, spriteComp)));
        }
    }

    public void Clear()
    {
        foreach (var layer in _layers)
        {
            if (layer.Dummy is { } dummy)
                _entMan.QueueDeleteEntity(dummy);
        }

        _layers.Clear();
    }

    public void Draw(DrawingHandleScreen handle, UIBox2 box)
    {
        var size = MathF.Min(box.Width, box.Height);

        foreach (var (layer, texture, dummy) in _layers)
        {
            var center = box.Center + layer.Offset * size;
            var layerSize = size * layer.Scale;

            if (texture is not null)
            {
                var half = new Vector2(layerSize / 2f);
                handle.DrawTextureRect(texture, new UIBox2(center - half, center + half), layer.Color);
                continue;
            }

            if (dummy is not { } ent || _entMan.Deleted(ent))
                continue;

            _sprite.ForceUpdate(ent);
            var scale = new Vector2(layerSize / EyeManager.PixelsPerMeter);
            var spriteOffset = ent.Comp.Offset * new Vector2(1, -1) * EyeManager.PixelsPerMeter * scale;
            handle.DrawEntity(ent, center - spriteOffset, scale, Angle.Zero, Angle.Zero, Direction.South, ent.Comp);
        }
    }
}
