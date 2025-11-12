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
    public float Energy = 0f;

    [DataField, AutoNetworkedField]
    public float EnergyMax = 100f;

    /// <summary>
    /// resource consumed for growth
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Resource = 0f;

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
    /// On which tiles can this plant grow? If empty - on any.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ContentTileDefinition>> SoilTile = new();
}

/// <summary>
/// Is called periodically on the plant. Use it to change resources and energy level of plant.
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
