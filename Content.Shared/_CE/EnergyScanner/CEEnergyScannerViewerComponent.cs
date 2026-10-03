using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.EnergyScanner;

/// <summary>
/// Applied to an entity currently wearing energy scanner glasses. Server-authoritative: added and removed by the
/// server on equip / unequip of <see cref="CEEnergyScannerClothingComponent"/>; the client only mirrors it to
/// show the pipe overlay. Widths are in world units (1 = one tile).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEEnergyScannerViewerComponent : Component
{
    [DataField]
    public float LargeWidth = 0.1f;

    [DataField]
    public Color LargeColor = Color.FromHex("#c5f1ff");

    [DataField]
    public float MediumWidth = 0.04f;

    [DataField]
    public Color MediumColor = Color.FromHex("#7fbfff");

    /// <summary>
    /// World-space shift of big pipe lines from the tile centre, so they don't overlap medium pipes on the same tile.
    /// </summary>
    [DataField]
    public Vector2 LargeOffset = new(-0.125f, 0.125f);

    /// <summary>
    /// World-space shift of medium pipe lines from the tile centre, opposite to <see cref="LargeOffset"/>.
    /// </summary>
    [DataField]
    public Vector2 MediumOffset = new(0.125f, -0.125f);

    [DataField]
    public Color UnpoweredColor = Color.FromHex("#b8b8b8");

    /// <summary>
    /// Broken pipes are drawn with this color regardless of whether their network is powered.
    /// </summary>
    [DataField]
    public Color BrokenColor = Color.FromHex("#FF3030");

    /// <summary>
    /// Peak alpha multiplier for pipes on the neighbouring z-level. Pulses down to <see cref="NeighborLevelMinAlpha"/>.
    /// </summary>
    [DataField]
    public float NeighborLevelAlpha = 0.1f;

    /// <summary>
    /// Lowest alpha multiplier of the neighbouring z-level pulse.
    /// </summary>
    [DataField]
    public float NeighborLevelMinAlpha = 0.02f;

    /// <summary>
    /// Period of the neighbouring z-level alpha pulse, in seconds.
    /// </summary>
    [DataField]
    public float NeighborLevelPulsePeriod = 4f;

    /// <summary>
    /// Width multiplier of the faint halo drawn under powered and broken pipes.
    /// </summary>
    [DataField]
    public float GlowWidthMultiplier = 3f;

    [DataField]
    public float GlowAlpha = 0.1f;

    /// <summary>
    /// Number of triangle arrows travelling along a z-level connection at once, evenly spaced in time.
    /// </summary>
    [DataField]
    public int VerticalArrowCount = 2;

    /// <summary>
    /// Time for one arrow to travel from the pipe centre to the end of its path, in seconds.
    /// </summary>
    [DataField]
    public float VerticalArrowPeriod = 1f;

    /// <summary>
    /// Length and base width of a z-level connection arrow.
    /// </summary>
    [DataField]
    public float VerticalArrowSize = 0.16f;

    /// <summary>
    /// How far an arrow travels from the pipe centre before fully fading out.
    /// </summary>
    [DataField]
    public float VerticalArrowDistance = 0.35f;
}
