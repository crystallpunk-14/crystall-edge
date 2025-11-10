using Content.Shared.Maps;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// The backbone of any plant. Provides common variables for the plant to other components
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState(true, fieldDeltas: true), Access(typeof(CESharedFarmingSystem))]
public sealed partial class CEPlantComponent : Component
{
    /// <summary>
    /// The ability to consume a resource for growing
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Energy = 30f;

    [DataField, AutoNetworkedField]
    public float EnergyMax = 100f;

    /// <summary>
    /// resource consumed for growth
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Resource = 30f;

    [DataField, AutoNetworkedField]
    public float ResourceMax = 100f;

    /// <summary>
    /// Plant growth status, from 0 to 1
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GrowthLevel;

    [DataField]
    public float UpdateFrequency = 60f;

    [DataField, AutoPausedField]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;

    /// <summary>
    /// Solution for metabolizing resources
    /// </summary>
    [DataField]
    public string? Solution;

    /// <summary>
    /// On which tiles can this plant grow? If empty - on any.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ContentTileDefinition>> SoilTile = new();

    /// <summary>
    /// What resource is collected when this plant is destroyed? While <see cref="CEPlantProducingComponent"/> provides additional
    /// harvests that grow periodically on the plant, this resource will be obtained from the plant itself when it is destroyed.
    /// The amount of harvest is scaled from <see cref="GrowthLevel"/>.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, int> DestructProduce = new();
}

/// <summary>
/// Is called periodically at random intervals on the plant.
/// </summary>
public sealed class CEPlantUpdateEvent(Entity<CEPlantComponent> comp) : EntityEventArgs
{
    public readonly Entity<CEPlantComponent> Plant = comp;
}

/// <summary>
/// is called after CEPlantUpdateEvent when all value changes have already been calculated.
/// </summary>
public sealed class CEAfterPlantUpdateEvent(Entity<CEPlantComponent> comp) : EntityEventArgs
{
    public readonly Entity<CEPlantComponent> Plant = comp;
}
