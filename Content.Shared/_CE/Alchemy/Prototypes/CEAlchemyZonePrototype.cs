using System.Numerics;
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
    /// Base center of the circle. The round seed shifts it, the shifted value lives in the round world.
    /// </summary>
    [DataField(required: true)]
    public Vector2 Position;

    [DataField(required: true)]
    public float Radius;

    /// <summary>
    /// Strength multiplier of the zone.
    /// </summary>
    [DataField]
    public float Gain = 1f;

    /// <summary>
    /// Harm of the zone: false is painted green (useful), true is painted orange (dangerous).
    /// The strength of a zone always comes from the length of the marker path inside its circle.
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
