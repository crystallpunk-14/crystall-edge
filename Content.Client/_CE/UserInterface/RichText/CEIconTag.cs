using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._CE.UserInterface.RichText;

/// <summary>
/// Inline icon in rich text: <c>[ceicon rsi="Objects/....rsi" state="brute"/]</c> or <c>[ceicon texture="Interface/....png"/]</c>,
/// paths relative to /Textures like in prototypes, optional <c>size</c> in pixels
/// </summary>
[UsedImplicitly]
public sealed partial class CEIconTag : IMarkupTagHandler
{
    [Dependency] private IEntitySystemManager _systems = default!;

    private const int DefaultSize = 16;

    /// <summary>
    /// The engine's safe default tag set (internal RichTextEntry.DefaultTags) plus this one
    /// </summary>
    public static readonly Type[] DefaultTagsWithIcon =
    [
        typeof(BoldItalicTag),
        typeof(BoldTag),
        typeof(BulletTag),
        typeof(ColorTag),
        typeof(HeadingTag),
        typeof(ItalicTag),
        typeof(CEIconTag),
    ];

    public string Name => "ceicon";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!TryGetSprite(node, out var sprite))
            return false;

        var size = DefaultSize;
        if (node.Attributes.TryGetValue("size", out var sizeParam) && sizeParam.TryGetLong(out var sizeValue))
            size = (int) sizeValue.Value;

        var texture = _systems.GetEntitySystem<SpriteSystem>().Frame0(sprite);

        control = new TextureRect
        {
            Texture = texture,
            SetSize = new Vector2(size, size),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            VerticalAlignment = Control.VAlignment.Center,
        };
        return true;
    }

    private static bool TryGetSprite(MarkupNode node, [NotNullWhen(true)] out SpriteSpecifier? sprite)
    {
        sprite = null;

        if (node.Attributes.TryGetValue("texture", out var textureParam) && textureParam.TryGetString(out var texture))
        {
            sprite = new SpriteSpecifier.Texture(new ResPath(texture));
            return true;
        }

        if (!node.Attributes.TryGetValue("rsi", out var rsiParam) || !rsiParam.TryGetString(out var rsi)
            || !node.Attributes.TryGetValue("state", out var stateParam) || !stateParam.TryGetString(out var state))
            return false;

        sprite = new SpriteSpecifier.Rsi(new ResPath(rsi), state);
        return true;
    }
}
