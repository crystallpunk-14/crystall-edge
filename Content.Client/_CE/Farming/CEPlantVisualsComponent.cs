using Robust.Shared.Prototypes;

namespace Content.Client._CE.Farming;

/// <summary>
/// Controls the visual display of plant growth
/// </summary>
[RegisterComponent]
public sealed partial class CEPlantVisualsComponent : Component
{
    /// <summary>
    /// The state of a gradually growing plant
    /// </summary>
    [DataField]
    public string GrowState = "grow-";

    [DataField]
    public string? GrowUnshadedState;

    /// <summary>
    /// Number of growth stage sprites.
    /// </summary>
    [DataField]
    public int GrowthSteps = 3;

    /// <summary>
    /// The state of a fully grown plant.
    /// </summary>
    [DataField]
    public string ReadyState = "grown-";

    [DataField]
    public string? ReadyUnshadedState;

    /// <summary>
    /// Number of variations of the sprite of a fully grown plant
    /// </summary>
    [DataField]
    public int ReadyVariations = 1;

    [DataField]
    public int? SelectedVariation = null;
}

public enum PlantVisualLayers : byte
{
    Base,
    BaseUnshaded,
    Produce,
}
