using Content.Shared._CE.EntityEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Alchemy.Prototypes;

/// <summary>
/// A circle on the brewing map with entity effects. Useful and dangerous zones are the same prototype
/// with different effects, they differ only by <see cref="Dangerous"/>.
/// </summary>
[Prototype("alchemyZone")]
public sealed partial class CEAlchemyZonePrototype : IPrototype
{
    /// <summary>
    /// Color of useful zones on the map, in the guidebook and in the scan.
    /// </summary>
    public static readonly Color UsefulColor = Color.FromHex("#4fb59a");

    /// <summary>
    /// Color of dangerous zones on the map, in the guidebook and in the scan.
    /// </summary>
    public static readonly Color DangerousColor = Color.FromHex("#e08a4a");

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Color of the effect. The mixture is painted with the colors of its active zones.
    /// </summary>
    [DataField(required: true)]
    public Color Color;

    /// <summary>
    /// Relative size of the zone in the round layout: zones grow in proportion to their weights until the map is full.
    /// Position and radius are not stored, the layout of the round places and sizes every zone.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Harm of the zone: false is painted green (useful), true is painted orange (dangerous).
    /// The strength of a zone always comes from how close the marker path came to its center.
    /// </summary>
    [DataField]
    public bool Dangerous;

    /// <summary>
    /// Color that shows the harm of the zone: green when useful, orange when dangerous.
    /// </summary>
    public Color HarmColor => Dangerous ? DangerousColor : UsefulColor;

    [DataField]
    public List<CEEntityEffect> Effects = new();
}
