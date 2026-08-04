using Robust.Shared.Noise;
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
    /// (but selectable) tile content.
    /// </summary>
    [DataField]
    public SpriteSpecifier MapBgLight = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Science/bg.rsi"), "light");

    [DataField]
    public SpriteSpecifier MapBgDark = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Science/bg.rsi"), "dark");

    [DataField]
    public SpriteSpecifier MapDeadZoneIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Science/dead.rsi"), "dead");

    /// <summary>
    /// Drawn for researched tiles with content whose specific icon we don't otherwise render
    /// (achievements, for now). Tinted with <see cref="Color"/> when drawn.
    /// </summary>
    [DataField]
    public SpriteSpecifier MapUnknownIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Science/unknown.rsi"), "unknown");

    /// <summary>
    /// Extra tiles of radius generated beyond the area's farthest achievement difficulty, so
    /// there's room for the "+1 ring" placement fallback and some dead-zone padding around the
    /// outermost achievements.
    /// </summary>
    [DataField]
    public int GenerationMargin = 2;

    /// <summary>
    /// Noise layers used to decide which tiles become dead zones when this area's map is
    /// procedurally generated at round start. A tile becomes a dead zone if any layer's noise
    /// value at that coordinate exceeds its threshold.
    /// </summary>
    [DataField]
    public List<CEScienceNoiseLayer> DeadZoneLayers = new();
}

[DataRecord]
public partial record struct CEScienceNoiseLayer
{
    /// <summary>
    /// If the noise value at a coordinate is above this, that coordinate becomes a dead zone.
    /// </summary>
    [DataField]
    public float Threshold;

    [DataField(required: true)]
    public FastNoiseLite Noise;
}
