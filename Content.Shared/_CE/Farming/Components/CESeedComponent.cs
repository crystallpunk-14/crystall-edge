using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// a component that allows for the creation of the entity on the tile
/// </summary>
[RegisterComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CESeedComponent : Component
{
    [DataField]
    public TimeSpan PlantingTime = TimeSpan.FromSeconds(1f);

    [DataField(required: true)]
    public EntProtoId PlantProto;
}
