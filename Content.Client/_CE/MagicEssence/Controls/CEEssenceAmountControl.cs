using System.Numerics;
using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.MagicEssence.Controls;

/// <summary>
/// A single essence type's icon with its amount rendered in the bottom-right corner, in the same
/// style (font, outline) as <see cref="Content.Client._CE.MagicEssence.CEMagicEssenceOverlay"/>'s
/// in-world readout. Reusable anywhere an amount of a specific essence needs to be shown as a UI
/// control rather than an in-world overlay (e.g. the research table's points wallet, action costs).
/// </summary>
public sealed partial class CEEssenceAmountControl : Control
{
    // The amount text renders at half the icon's size, matching CEMagicEssenceOverlay.
    private const float TextScaleMultiplier = 0.5f;
    private const float OutlineOffset = 1f;

    private static readonly Color OutlineColor = Color.Black.WithAlpha(0.85f);
    private static readonly Color TextColor = Color.White;

    private static readonly Vector2 OLeft = new(-OutlineOffset, 0f);
    private static readonly Vector2 ORight = new(OutlineOffset, 0f);
    private static readonly Vector2 OUp = new(0f, -OutlineOffset);
    private static readonly Vector2 ODown = new(0f, OutlineOffset);

    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IResourceCache _resourceCache = default!;

    private readonly Font _font;

    private Texture? _texture;
    private int _amount;

    public CEEssenceAmountControl()
    {
        IoCManager.InjectDependencies(this);

        var fontResource = _resourceCache.GetResource<FontResource>("/Fonts/_CE/Volkorn/VollkornSC-Bold.ttf");
        _font = new VectorFont(fontResource, 12);
    }

    /// <summary>
    /// Sets which essence type and amount to display. Safe to call repeatedly to update in place.
    /// </summary>
    public void SetEssence(ProtoId<CEMagicEssenceTypePrototype> essence, int amount)
    {
        _amount = amount;

        if (_prototype.TryIndex(essence, out CEMagicEssenceTypePrototype? proto))
        {
            _texture = proto.Icon.Frame0();
            ToolTip = proto.Name;
        }
        else
        {
            _texture = null;
            ToolTip = essence.Id;
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_texture is null)
            return;

        handle.DrawTextureRect(_texture, PixelSizeBox);

        var textScale = UIScale * TextScaleMultiplier;
        var ascent = _font.GetAscent(textScale);

        var text = _amount.ToString();
        var textDims = handle.GetDimensions(_font, text, textScale);

        // Anchor by baseline (icon bottom - ascent) rather than full line height, since digits
        // have no descenders and the line-height box would otherwise sit too high.
        var textPos = new Vector2(PixelWidth - textDims.X, PixelHeight - ascent);

        handle.DrawString(_font, textPos + OLeft * textScale, text, textScale, OutlineColor);
        handle.DrawString(_font, textPos + ORight * textScale, text, textScale, OutlineColor);
        handle.DrawString(_font, textPos + OUp * textScale, text, textScale, OutlineColor);
        handle.DrawString(_font, textPos + ODown * textScale, text, textScale, OutlineColor);
        handle.DrawString(_font, textPos, text, textScale, TextColor);
    }
}
