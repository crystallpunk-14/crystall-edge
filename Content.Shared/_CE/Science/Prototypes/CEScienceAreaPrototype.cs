using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Science.Prototypes;

[Prototype("scienceArea")]
public sealed partial class CEScienceAreaPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Desc;

    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Parallax prototype shown behind this area's research map. Empty means no parallax - a
    /// plain black background is drawn instead.
    /// </summary>
    [DataField]
    public string Parallax = string.Empty;

    /// <summary>
    /// Research map theme: background checkerboard, and icons for dead zones and unresearched
    /// (but selectable) cell content.
    /// </summary>
    [DataField]
    public SpriteSpecifier MapBgLight = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Science/bg.rsi"), "light");

    [DataField]
    public SpriteSpecifier MapBgDark = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Science/bg.rsi"), "dark");

    [DataField]
    public SpriteSpecifier MapDeadZoneIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Science/dead.rsi"), "dead");

    /// <summary>
    /// Drawn for researched cells with content whose specific icon we don't otherwise render
    /// (achievements, for now). Tinted with <see cref="Color"/> when drawn.
    /// </summary>
    [DataField]
    public SpriteSpecifier MapUnknownIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Science/unknown.rsi"), "unknown");
}
