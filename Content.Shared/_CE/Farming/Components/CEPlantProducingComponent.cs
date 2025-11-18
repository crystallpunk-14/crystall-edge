using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// Once this plant has grown, it begins to grow fruit inside itself.
/// Extraction methods are determined by other components. For example, PlantGatherOnInteractComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEPlantProducingComponent : Component
{
    /// <summary>
    /// what types of crops does this plant produce, and how
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, CEGatherEntry> GatherKeys = new();

    /// <summary>
    /// Random shift of the appearing entity during gathering
    /// </summary>
    [DataField]
    public float GatherOffset = 0.7f;
}

[Serializable]
[DataDefinition]
public sealed partial class CEGatherEntry
{
    [DataField(required: true)]
    public EntProtoId Result;

    /// <summary>
    /// How much has this type of resource grown on this plant? From 0 to 1.
    /// </summary>
    [DataField]
    public float Growth = 0f;

    /// <summary>
    /// How much fruit can be harvested? Regarding the <see cref="Growth"/> level
    /// </summary>
    [DataField]
    public int MaxProduce = 1;

    /// <summary>
    /// How many resources are spent on growing this resource COMPLETELY?
    /// This means that this number will be multiplied by <see cref="GrowthPerUpdate"/>.
    /// </summary>
    [DataField]
    public float ResourceCost = 1f;

    /// <summary>
    /// How many energy are spent on growing this resource COMPLETELY?
    /// This means that this number will be multiplied by <see cref="GrowthPerUpdate"/>.
    /// </summary>
    [DataField]
    public float EnergyCost = 1f;

    /// <summary>
    /// How much resource will be grown per plant update tick?
    /// If the maximum refresh tick is 1 minute and this number is 0.1, it means that this resource will grow completely in a maximum of 10 minutes.
    /// </summary>
    /// <remarks>If there are insufficient resources or energy for fruit growth, they will not be spent, and this growth update tick will not occur.</remarks>
    [DataField]
    public float GrowthPerUpdate = 0.1f;

    [DataField]
    public int? VisualStateCount;
}

/// <summary>
/// A common interface for components that are different ways of gathering resources from PlantGatherableComponent
/// </summary>
public interface IPlantGatherMethod
{
    /// <summary>
    /// What types of resources are gathered using this component method?
    /// </summary>
    public HashSet<string> GatherKeys { get; set; }
}
