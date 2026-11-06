using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Footprints;

/// <summary>
/// Lets the entity pick up reagents from puddles with its feet (or body, while lying) and leave footprints behind.
/// Feet reagents live on worn shoes with <see cref="CEFootprintShoesComponent"/> if there are any, otherwise in <see cref="FeetSolution"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEFootprintOwnerComponent : Component
{
    [DataField]
    public EntProtoId FootprintProto = "CEFootprint";

    /// <summary>
    /// Barefoot print sprite of this species.
    /// </summary>
    [DataField]
    public SpriteSpecifier FootSprite = new SpriteSpecifier.Rsi(new ResPath("_CE/Effects/Footprints/feet.rsi"), "human");

    /// <summary>
    /// Smear sprite left while the entity is lying and moving (dragged body).
    /// </summary>
    [DataField]
    public SpriteSpecifier BodySprite = new SpriteSpecifier.Rsi(new ResPath("_CE/Effects/Footprints/drag.rsi"), "body");

    [DataField]
    public Solution FeetSolution = new();

    [DataField]
    public Solution BodySolution = new();

    /// <summary>
    /// How many reagents bare feet can carry.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxFeetVolume = 10;

    /// <summary>
    /// How many reagents the body can carry while lying.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxBodyVolume = 20;

    /// <summary>
    /// Max volume picked up from a puddle per step.
    /// </summary>
    [DataField]
    public FixedPoint2 PickupVolume = 5;

    [DataField]
    public FixedPoint2 MinFootprintVolume = 0.5f;

    [DataField]
    public FixedPoint2 MaxFootprintVolume = 1;

    [DataField]
    public FixedPoint2 MinBodyprintVolume = 2;

    [DataField]
    public FixedPoint2 MaxBodyprintVolume = 5;

    /// <summary>
    /// Distance between footprints. One footprint sprite contains a pair of steps.
    /// </summary>
    [DataField]
    public float FootDistance = 1f;

    [DataField]
    public float BodyDistance = 1f;

    /// <summary>
    /// Opacity of a footprint left with the maximum volume.
    /// </summary>
    [DataField]
    public float MaxAlpha = 0.8f;

    /// <summary>
    /// Distance travelled since the last print.
    /// </summary>
    [DataField]
    public float Distance;
}
