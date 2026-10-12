using Content.Server._CE.Procedural.Generation;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Wildlands;

/// <summary>
/// Full wildlands setup of one map: landscape around the city and modifier tables for wildlands and city.
/// </summary>
[Prototype("wildlandsConfig")]
public sealed partial class CEWildlandsConfigPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Origin of every layer's sampled area and noise.
    /// </summary>
    [DataField]
    public Vector2i Center;

    /// <summary>
    /// Landscape layers keyed by real z-network depth (main map = 0).
    /// </summary>
    [DataField]
    public Dictionary<int, List<ICEProceduralLayer>> LayersByDepth = new();

    /// <summary>
    /// Picks modifier entity prototypes applied outside the city.
    /// </summary>
    [DataField]
    public EntityTableSelector? WildlandsModifiers;

    /// <summary>
    /// Picks modifier entity prototypes applied inside the city.
    /// </summary>
    [DataField]
    public EntityTableSelector? CityModifiers;
}
