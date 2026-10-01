namespace Content.Shared._CE.EnergyScanner;

/// <summary>
/// Marker for clothing that grants energy scanner vision while worn in the eyes slot: a pipe overlay across z-levels
/// and detailed power network readings on examine. The wearer receives <see cref="CEEnergyScannerViewerComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEEnergyScannerClothingComponent : Component;
