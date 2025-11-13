using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// Once the plant has fully grown,
/// it begins to attempt to expend resources to grow the specified other plant (most likely itself) on adjacent tiles.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantKudzuComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Proto;

    /// <summary>
    /// The amount of resources required in a plant to grow on the adjacent tile
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ResourceCost = 80f;

    /// <summary>
    /// The amount of energy required in a plant to grow on the adjacent tile
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EnergyCost = 80f;
}
