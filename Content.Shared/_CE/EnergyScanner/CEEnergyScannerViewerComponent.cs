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
    public float LargeWidth = 0.15f;

    [DataField]
    public Color LargeColor = Color.FromHex("#80dfff");

    [DataField]
    public float MediumWidth = 0.07f;

    [DataField]
    public Color MediumColor = Color.FromHex("#7fbfff");

    [DataField]
    public Color UnpoweredColor = Color.FromHex("#b8b8b8");

    /// <summary>
    /// Broken pipes are drawn with this color regardless of whether their network is powered.
    /// </summary>
    [DataField]
    public Color BrokenColor = Color.FromHex("#FF3030");

    /// <summary>
    /// Alpha multiplier for pipes on the neighbouring z-level.
    /// </summary>
    [DataField]
    public float NeighborLevelAlpha = 0.1f;

    /// <summary>
    /// Width multiplier of the faint halo drawn under powered and broken pipes.
    /// </summary>
    [DataField]
    public float GlowWidthMultiplier = 3f;

    [DataField]
    public float GlowAlpha = 0.1f;
}
