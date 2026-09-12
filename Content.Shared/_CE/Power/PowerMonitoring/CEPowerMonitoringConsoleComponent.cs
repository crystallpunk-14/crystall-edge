using Content.Shared.Power;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Power.PowerMonitoring;

/// <summary>
/// Flags an entity as being a multi z-level power monitoring console.
/// CE fork of <c>Content.Shared.Power.PowerMonitoringConsoleComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CESharedPowerMonitoringConsoleSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class CEPowerMonitoringConsoleComponent : Component
{
    /// <summary>
    /// The EntityUid of the device that is the console's current focus. Not networked - set by the console UI.
    /// </summary>
    [ViewVariables]
    public EntityUid? Focus;

    /// <summary>
    /// The group that the focused device belongs to. Not networked - set by the console UI.
    /// </summary>
    [ViewVariables]
    public PowerMonitoringConsoleGroup FocusGroup = PowerMonitoringConsoleGroup.Generator;

    /// <summary>
    /// Flags for currently active events of interest (rogue consumers, power net anomalies, energy leaks).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public CEPowerMonitoringFlags Flags = CEPowerMonitoringFlags.None;

    /// <summary>
    /// Meta data for every tracked power monitoring device across every z-level of the console's network.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<NetEntity, PowerMonitoringDeviceMetaData> PowerMonitoringDeviceMetaData = new();

    /// <summary>
    /// Locations of currently leaking power entities (<c>CEEnergyLeakComponent</c>) across the network,
    /// so the console can blip them on the map — the CE analogue of the rogue-power-consumer alert.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<NetEntity, NetCoordinates> EnergyLeaks = new();
}

[Flags]
public enum CEPowerMonitoringFlags : byte
{
    None = 0,
    RoguePowerConsumer = 1 << 0,
    PowerNetAbnormalities = 1 << 1,
    EnergyLeak = 1 << 2,
}

/// <summary>
/// Data sent by the server to the client for the CE power monitoring console UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEPowerMonitoringConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public double TotalSources;
    public double TotalBatteryUsage;
    public double TotalLoads;
    public PowerMonitoringConsoleEntry[] AllEntries;
    public PowerMonitoringConsoleEntry[] FocusSources;
    public PowerMonitoringConsoleEntry[] FocusLoads;

    public CEPowerMonitoringConsoleBoundInterfaceState(
        double totalSources,
        double totalBatteryUsage,
        double totalLoads,
        PowerMonitoringConsoleEntry[] allEntries,
        PowerMonitoringConsoleEntry[] focusSources,
        PowerMonitoringConsoleEntry[] focusLoads)
    {
        TotalSources = totalSources;
        TotalBatteryUsage = totalBatteryUsage;
        TotalLoads = totalLoads;
        AllEntries = allEntries;
        FocusSources = focusSources;
        FocusLoads = focusLoads;
    }
}

/// <summary>
/// Triggers the server to send updated console data for this player's session.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEPowerMonitoringConsoleMessage : BoundUserInterfaceMessage
{
    public NetEntity? FocusDevice;
    public PowerMonitoringConsoleGroup FocusGroup;

    public CEPowerMonitoringConsoleMessage(NetEntity? focusDevice, PowerMonitoringConsoleGroup focusGroup)
    {
        FocusDevice = focusDevice;
        FocusGroup = focusGroup;
    }
}

[Serializable, NetSerializable]
public enum CEPowerMonitoringConsoleUiKey : byte
{
    Key,
}
