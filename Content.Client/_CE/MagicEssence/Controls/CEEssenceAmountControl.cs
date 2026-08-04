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
    private const float TextScaleMultiplier = 2f;
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

        // Tooltips (the essence's name, set in SetEssence) only show up for controls that
        // actually accept mouse input - the default MouseFilter.Ignore lets hover events pass
        // straight through.
        MouseFilter = MouseFilterMode.Stop;

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
            ToolTip = $"{proto.Name} x{amount}";
        }
        else
        {
            _texture = null;
            ToolTip = $"{essence.Id} x{amount}";
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_texture is null)
            return;

        handle.DrawTextureRect(_texture, PixelSizeBox);

        var text = _amount.ToString();
        var baseTextScale = UIScale * TextScaleMultiplier;
        var unscaledWidth = handle.GetDimensions(_font, text, baseTextScale).X;

        // Shrink the font as more digits are added so the amount always fits within the
        // control's width instead of overflowing it - one digit renders at full size, each
        // additional digit shrinks the whole readout further.
        var textScale = PixelWidth > 0f && unscaledWidth > PixelWidth
            ? baseTextScale * (PixelWidth / unscaledWidth)
            : baseTextScale;

        var ascent = _font.GetAscent(textScale);
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
