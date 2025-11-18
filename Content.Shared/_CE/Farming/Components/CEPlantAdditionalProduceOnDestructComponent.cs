using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// What resource is collected when this plant is destroyed? While <see cref="CEPlantProducingComponent"/> provides additional
/// harvests that grow periodically on the plant, this resource will be obtained from the plant itself when it is destroyed.
/// The amount of harvest is scaled from GrowthLevel of <see cref="CEPlantComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantAdditionalProduceOnDestructComponent : Component
{
    [DataField]
    public Dictionary<EntProtoId, int> Produce = new();
}


/// <summary>
/// What resource is collected when this plant is interacted? While <see cref="CEPlantProducingComponent"/> provides additional
/// harvests that grow periodically on the plant, this resource will be obtained from the plant itself when it is destroyed.
/// The amount of harvest is scaled from GrowthLevel of <see cref="CEPlantComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantAdditionalProduceOnInteractComponent : Component
{
    [DataField]
    public Dictionary<EntProtoId, int> Produce = new();

    /// <summary>
    /// Whitelist for specifying the kind of tools can be used on a resource
    /// Supports multiple tags.
    /// If null, no whitelist checking.
    /// </summary>
    [DataField]
    public EntityWhitelist? ToolWhitelist;

    [DataField]
    public TimeSpan GatherDelay = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// Sound to play when gathering
    /// </summary>
    [DataField]
    public SoundSpecifier GatherSound = new SoundCollectionSpecifier("CEGrassGathering");
}
