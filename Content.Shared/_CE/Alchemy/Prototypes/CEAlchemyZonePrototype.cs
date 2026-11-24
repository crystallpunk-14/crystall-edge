using System.Numerics;
using Content.Shared._CE.EntityEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Alchemy.Prototypes;

/// <summary>
/// A circle on the brewing map with entity effects. The code does not know whether the zone is good or bad:
/// targets, side effects and bonuses are the same prototype with different effects.
/// </summary>
[Prototype("alchemyZone")]
public sealed partial class CEAlchemyZonePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

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
    /// False: the zone works by where the marker stopped, strength falls from the center to the edge.
    /// True: the zone works by the length of the marker path inside the circle.
    /// </summary>
    [DataField]
    public bool ByPath;

    [DataField]
    public List<CEEntityEffect> Effects = new();
}
