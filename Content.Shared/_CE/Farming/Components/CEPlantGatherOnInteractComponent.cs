using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// Stores information about resources that can be extracted from a plant.
/// Extraction methods are determined by other components. For example, PlantGatherOnInteractComponent.
/// </summary>
[RegisterComponent]
public sealed partial class CEPlantGatherOnInteractComponent : Component, IPlantGatherMethod
{
    [DataField]
    public HashSet<string> GatherKeys { get; set; } = new();

    /// <summary>
    /// Whitelist for specifying the kind of tools can be used on a resource
    /// Supports multiple tags.
    /// If null, no whitelist checking.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist ToolWhitelist = new();

    [DataField]
    public TimeSpan GatherDelay = TimeSpan.FromSeconds(1f);

    [DataField]
    public float GatherAmount = 1f;

    /// <summary>
    /// Sound to play when gathering
    /// </summary>
    [DataField]
    public SoundSpecifier GatherSound = new SoundCollectionSpecifier("CEGrassGathering");
}
