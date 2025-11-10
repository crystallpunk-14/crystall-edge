using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// What resource is collected when this plant is destroyed? While <see cref="CEPlantProducingComponent"/> provides additional
/// harvests that grow periodically on the plant, this resource will be obtained from the plant itself when it is destroyed.
/// The amount of harvest is scaled from GrowthLevel of <see cref="CEPlantComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantSelfProduceComponent : Component
{
    [DataField]
    public Dictionary<EntProtoId, int> Produce = new();
}
