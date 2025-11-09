namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// Is trying to use up the plant's energy and resources to grow.
/// </summary>
[RegisterComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantGrowingComponent : Component
{
    [DataField]
    public float EnergyCost = 0f;

    [DataField]
    public float ResourceCost = 0f;

    /// <summary>
    /// for each plant renewal. It is not every frame, it depends on the refresh rate in PlantComponent
    /// </summary>
    [DataField]
    public float GrowthPerUpdate = 0f;
}
