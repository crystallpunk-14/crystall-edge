using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.ZLevels.Weather;

/// <summary>
/// Automatic weather for the station's z-network: a graph of weather nodes with weighted transitions between them.
/// Driven by <see cref="CEStationWeatherSystem"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEStationWeatherComponent : Component
{
    /// <summary>
    /// When false, the automatic weather is paused and the weather is left untouched.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Weather graph. Transitions reference other nodes by their key.
    /// The starting node is picked uniformly at random among nodes with <see cref="CEWeatherNode.RoundStart"/>.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, CEWeatherNode> Nodes = new();

    /// <summary>
    /// Key of the node that is active right now. Null until the first update picks a starting node.
    /// </summary>
    [DataField]
    public string? CurrentNode;

    /// <summary>
    /// The time at which the current node ends and the next one is picked.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextChangeTime;
}

/// <summary>
/// A single state of the weather graph.
/// </summary>
[DataDefinition]
public sealed partial class CEWeatherNode
{
    /// <summary>
    /// Weather to set while this node is active. Null means clear sky.
    /// </summary>
    [DataField]
    public EntProtoId? Weather;

    /// <summary>
    /// Whether this node can be picked as the starting node of the round.
    /// </summary>
    [DataField]
    public bool RoundStart;

    [DataField]
    public TimeSpan MinDuration = TimeSpan.FromMinutes(5);

    [DataField]
    public TimeSpan MaxDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Node key -> relative weight of moving to that node when this one ends.
    /// </summary>
    [DataField]
    public Dictionary<string, float> Transitions = new();
}
