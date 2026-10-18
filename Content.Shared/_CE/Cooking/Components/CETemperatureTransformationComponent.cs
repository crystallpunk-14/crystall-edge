using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Cooking.Components;

/// <summary>
/// Replaces this entity with another one when its temperature reaches one of the specified ranges (e.g. raw meat becomes cooked).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CETemperatureTransformationComponent : Component
{
    [DataField(required: true)]
    public List<CETemperatureTransformEntry> Entries = new();

    /// <summary>
    /// The transformation will occur instantly if the entity was in FoodCookerComponent at the moment cooking ended.
    /// </summary>
    [DataField]
    public bool AutoTransformOnCooked = true;
}

[DataRecord]
public partial record struct CETemperatureTransformEntry()
{
    public EntProtoId? TransformTo { get; set; } = null;
    public Vector2 TemperatureRange { get; set; } = new();
}
