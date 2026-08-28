using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Power;

namespace Content.Server._CE.Power.PowerMonitoring;

/// <summary>
/// CE fork of <c>Content.Server.Power.Components.PowerMonitoringDeviceComponent</c>. Flags a CE power
/// structure so it appears on the multi z-level power monitoring console. Owned by the CE system, so
/// it can be freely written and subscribed to without clashing with the upstream console system.
/// </summary>
[RegisterComponent, Access(typeof(CEPowerMonitoringConsoleSystem))]
public sealed partial class CEPowerMonitoringDeviceComponent : Component
{
    /// <summary>Name of the node this device draws power from (see <see cref="Content.Server.NodeContainer.NodeContainerComponent"/>).</summary>
    [DataField("sourceNode")]
    public string SourceNode = string.Empty;

    /// <summary>Name of the node this device distributes power to.</summary>
    [DataField("loadNode")]
    public string LoadNode = string.Empty;

    /// <summary>Names of the nodes this device can potentially distribute power to.</summary>
    [DataField("loadNodes")]
    public List<string>? LoadNodes;

    /// <summary>This entity is grouped with entities that share this collection name.</summary>
    [DataField("collectionName")]
    public string CollectionName = string.Empty;

    [ViewVariables]
    public BaseNodeGroup? NodeGroup;

    public bool IsCollectionMasterOrChild => CollectionName != string.Empty;

    /// <summary>The uid of the master that represents this entity when grouping multiple entities.</summary>
    [ViewVariables]
    public EntityUid CollectionMaster;

    public bool IsCollectionMaster => Owner == CollectionMaster;

    /// <summary>Entities represented by this entity when grouped.</summary>
    [ViewVariables]
    public Dictionary<EntityUid, CEPowerMonitoringDeviceComponent> ChildDevices = new();

    /// <summary>Path to the .rsi folder for the list icon.</summary>
    [DataField("sprite")]
    public string SpritePath = string.Empty;

    /// <summary>The .rsi state for the list icon.</summary>
    [DataField("state")]
    public string SpriteState = string.Empty;

    /// <summary>Which power monitoring group / tab this entity belongs to.</summary>
    [DataField("group", required: true)]
    public PowerMonitoringConsoleGroup Group;
}
