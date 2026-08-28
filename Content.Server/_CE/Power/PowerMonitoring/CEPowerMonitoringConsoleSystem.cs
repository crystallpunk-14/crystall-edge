using System.Linq;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Server.Power.NodeGroups;
using Content.Server.StationEvents.Components;
using Content.Shared._CE.Power.Components;
using Content.Shared._CE.Power.PowerMonitoring;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.NodeContainer;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server._CE.Power.PowerMonitoring;

/// <summary>
/// CE fork of <c>Content.Server.Power.EntitySystems.PowerMonitoringConsoleSystem</c>. Tracks power
/// devices and cable networks across every z-level grid in the console's z-network, feeding the
/// multi-level <c>CEPowerMonitoringConsoleNavMapControl</c>.
/// </summary>
[UsedImplicitly]
public sealed partial class CEPowerMonitoringConsoleSystem : CESharedPowerMonitoringConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly CESharedZLevelsSystem _zLevels = default!;

    // Note: this data does not need to be saved
    private readonly Dictionary<EntityUid, Dictionary<Vector2i, PowerCableChunk>> _gridPowerCableChunks = new();
    private float _updateTimer = 1.0f;

    private const float UpdateTime = 1.0f;
    private const float RoguePowerConsumerThreshold = 100000;

    // Any leak above this (watts) trips the EnergyLeak warning and blips the source on the map.
    private const float EnergyLeakWarningThreshold = 0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEPowerMonitoringConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<CEPowerMonitoringConsoleComponent, EntParentChangedMessage>(OnConsoleParentChanged);
        SubscribeLocalEvent<CEPowerMonitoringCableNetworksComponent, ComponentInit>(OnCableNetworksInit);
        SubscribeLocalEvent<CEPowerMonitoringCableNetworksComponent, EntParentChangedMessage>(OnCableNetworksParentChanged);

        SubscribeLocalEvent<CEPowerMonitoringConsoleComponent, CEPowerMonitoringConsoleMessage>(OnPowerMonitoringConsoleMessage);
        SubscribeLocalEvent<CEPowerMonitoringConsoleComponent, BoundUIOpenedEvent>(OnBoundUIOpened);

        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);

        SubscribeLocalEvent<CECableComponent, MapInitEvent>(OnCableMapInit);
        SubscribeLocalEvent<CECableComponent, AnchorStateChangedEvent>(OnCableAnchorChanged);
        SubscribeLocalEvent<CECableComponent, ComponentShutdown>(OnCableShutdown);

        SubscribeLocalEvent<CECableCutComponent, MapInitEvent>(OnCableCutChanged);
        SubscribeLocalEvent<CECableCutComponent, AnchorStateChangedEvent>(OnCableCutChanged);
        SubscribeLocalEvent<CECableCutComponent, ComponentShutdown>(OnCableCutRemoved);

        SubscribeLocalEvent<CEPowerMonitoringDeviceComponent, MapInitEvent>(OnDeviceMapInit);
        SubscribeLocalEvent<CEPowerMonitoringDeviceComponent, AnchorStateChangedEvent>(OnDeviceAnchorChanged);
        SubscribeLocalEvent<CEPowerMonitoringDeviceComponent, ComponentShutdown>(OnDeviceShutdown);
        SubscribeLocalEvent<CEPowerMonitoringDeviceComponent, NodeGroupsRebuilt>(OnNodeGroupRebuilt);

        SubscribeLocalEvent<GameRuleStartedEvent>(OnPowerGridCheckStarted);
        SubscribeLocalEvent<GameRuleEndedEvent>(OnPowerGridCheckEnded);
    }

    #region Network helpers

    /// <summary>Every z-level grid that belongs to the console's z-network (falls back to the console's own grid).</summary>
    private List<EntityUid> GetNetworkGrids(EntityUid consoleUid, TransformComponent? xform = null)
    {
        var result = new List<EntityUid>();

        if (!Resolve(consoleUid, ref xform))
            return result;

        if (xform.MapUid is { } mapUid && _zLevels.TryGetMapNetwork(mapUid, out var network))
        {
            foreach (var level in network.Comp.SortedZLevels)
            {
                if (level.IsValid() && HasComp<MapGridComponent>(level))
                    result.Add(level);
            }
        }

        if (result.Count == 0 && xform.GridUid is { } gridUid)
            result.Add(gridUid);

        return result;
    }

    private bool IsGridInConsoleNetwork(EntityUid consoleUid, EntityUid gridUid)
        => GetNetworkGrids(consoleUid).Contains(gridUid);

    #endregion

    #region Event handling

    private void OnConsoleInit(EntityUid uid, CEPowerMonitoringConsoleComponent component, ComponentInit args)
    {
        RefreshPowerMonitoringConsole(uid, component);
    }

    private void OnConsoleParentChanged(EntityUid uid, CEPowerMonitoringConsoleComponent component, EntParentChangedMessage args)
    {
        RefreshPowerMonitoringConsole(uid, component);
    }

    private void OnCableNetworksInit(EntityUid uid, CEPowerMonitoringCableNetworksComponent component, ComponentInit args)
    {
        RefreshPowerMonitoringCableNetworks(uid, component);
    }

    private void OnCableNetworksParentChanged(EntityUid uid, CEPowerMonitoringCableNetworksComponent component, EntParentChangedMessage args)
    {
        RefreshPowerMonitoringCableNetworks(uid, component);
    }

    private void OnPowerMonitoringConsoleMessage(EntityUid uid, CEPowerMonitoringConsoleComponent component, CEPowerMonitoringConsoleMessage args)
    {
        var focus = GetEntity(args.FocusDevice);

        if (component.Focus != focus)
        {
            component.Focus = focus;

            if (TryComp<CEPowerMonitoringCableNetworksComponent>(uid, out var cableNetworks))
            {
                cableNetworks.FocusChunks.Clear();

                if (focus == null)
                    Dirty(uid, cableNetworks);
            }
        }

        if (component.FocusGroup != args.FocusGroup)
        {
            component.FocusGroup = args.FocusGroup;
            Dirty(uid, component);
        }
    }

    private void OnBoundUIOpened(EntityUid uid, CEPowerMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        component.Focus = null;
        component.FocusGroup = PowerMonitoringConsoleGroup.Generator;

        if (TryComp<CEPowerMonitoringCableNetworksComponent>(uid, out var cableNetworks))
        {
            cableNetworks.FocusChunks.Clear();
            Dirty(uid, cableNetworks);
        }
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        var allGrids = args.NewGrids.ToList();

        if (!allGrids.Contains(args.Grid))
            allGrids.Add(args.Grid);

        foreach (var grid in allGrids)
        {
            if (!TryComp<MapGridComponent>(grid, out var map))
                continue;

            RefreshPowerCableGrid(grid, map);
        }

        var query = AllEntityQuery<CEPowerMonitoringConsoleComponent, CEPowerMonitoringCableNetworksComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entCableNetworks, out _))
        {
            if (!allGrids.Any(g => IsGridInConsoleNetwork(ent, g)))
                continue;

            RefreshPowerMonitoringConsole(ent, entConsole);
            RefreshPowerMonitoringCableNetworks(ent, entCableNetworks);
        }
    }

    private void OnCableMapInit(EntityUid uid, CECableComponent component, MapInitEvent args)
    {
        SetCableBit(uid, Transform(uid).Anchored);
        RefreshVerticalIfNeeded(uid);
    }

    private void OnCableAnchorChanged(EntityUid uid, CECableComponent component, AnchorStateChangedEvent args)
    {
        SetCableBit(uid, args.Anchored);
        RefreshVerticalIfNeeded(uid);
    }

    private void OnCableShutdown(EntityUid uid, CECableComponent component, ComponentShutdown args)
    {
        SetCableBit(uid, false);
        RefreshVerticalIfNeeded(uid, ignore: uid);
    }

    /// <summary>Rebuilds a grid's vertical-pipe list, but only when the changed cable actually connects z-levels.</summary>
    private void RefreshVerticalIfNeeded(EntityUid uid, EntityUid? ignore = null)
    {
        if (!TryGetVerticalPipe(uid, out _))
            return;

        if (Transform(uid).GridUid is { } gridUid)
            RebuildGridVerticalPipes(gridUid, ignore);
    }

    private bool TryGetVerticalPipe(EntityUid uid, out (bool Up, bool Down) dirs)
    {
        dirs = default;

        if (!TryComp<NodeContainerComponent>(uid, out var nodeContainer))
            return false;

        var up = false;
        var down = false;

        foreach (var node in nodeContainer.Nodes.Values)
        {
            if (node is CECableVerticalNode vertical)
            {
                up |= vertical.Up;
                down |= vertical.Down;
            }
        }

        dirs = (up, down);
        return up || down;
    }

    private List<CEVerticalPipe> BuildGridVerticalPipeList(EntityUid gridUid, EntityUid? ignore = null)
    {
        var result = new List<CEVerticalPipe>();

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return result;

        var query = AllEntityQuery<CableComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var cable, out var xform))
        {
            if (ent == ignore || xform.GridUid != gridUid || !xform.Anchored)
                continue;

            if (!TryGetVerticalPipe(ent, out var dirs))
                continue;

            result.Add(new CEVerticalPipe
            {
                Tile = _sharedMapSystem.LocalToTile(gridUid, grid, xform.Coordinates),
                Voltage = (byte) cable.CableType,
                Up = dirs.Up,
                Down = dirs.Down,
            });
        }

        return result;
    }

    private void RebuildGridVerticalPipes(EntityUid gridUid, EntityUid? ignore = null)
    {
        var pipes = BuildGridVerticalPipeList(gridUid, ignore);
        var gridNetEntity = GetNetEntity(gridUid);

        var query = AllEntityQuery<CEPowerMonitoringCableNetworksComponent>();
        while (query.MoveNext(out var ent, out var entCableNetworks))
        {
            if (!IsGridInConsoleNetwork(ent, gridUid))
                continue;

            entCableNetworks.VerticalPipes[gridNetEntity] = pipes;
            Dirty(ent, entCableNetworks);
        }
    }

    /// <summary>Sets or clears the cable bit for <paramref name="uid"/> in its grid's cached chunk and dirties affected consoles.</summary>
    private void SetCableBit(EntityUid uid, bool set)
    {
        if (!TryComp<CableComponent>(uid, out var cable))
            return;

        var xform = Transform(uid);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        if (!_gridPowerCableChunks.TryGetValue(gridUid, out var allChunks))
        {
            allChunks = new();
            _gridPowerCableChunks[gridUid] = allChunks;
        }

        var tile = _sharedMapSystem.LocalToTile(gridUid, grid, xform.Coordinates);
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, ChunkSize);

        if (!allChunks.TryGetValue(chunkOrigin, out var chunk))
        {
            chunk = new PowerCableChunk(chunkOrigin);
            allChunks[chunkOrigin] = chunk;
        }

        var flag = GetFlag(SharedMapSystem.GetChunkRelative(tile, ChunkSize));

        if (set)
            chunk.PowerCableData[(int) cable.CableType] |= flag;
        else
            chunk.PowerCableData[(int) cable.CableType] &= ~flag;

        var gridNetEntity = GetNetEntity(gridUid);

        var query = AllEntityQuery<CEPowerMonitoringCableNetworksComponent>();
        while (query.MoveNext(out var ent, out var entCableNetworks))
        {
            if (!IsGridInConsoleNetwork(ent, gridUid))
                continue;

            entCableNetworks.AllChunks[gridNetEntity] = allChunks;
            Dirty(ent, entCableNetworks);
        }
    }

    private void OnCableCutChanged(EntityUid uid, CECableCutComponent component, MapInitEvent args)
    {
        RebuildCutsForEntity(uid);
    }

    private void OnCableCutChanged(EntityUid uid, CECableCutComponent component, AnchorStateChangedEvent args)
    {
        RebuildCutsForEntity(uid);
    }

    private void OnCableCutRemoved(EntityUid uid, CECableCutComponent component, ComponentShutdown args)
    {
        RebuildCutsForEntity(uid, ignore: uid);
    }

    private void RebuildCutsForEntity(EntityUid uid, EntityUid? ignore = null)
    {
        if (Transform(uid).GridUid is { } gridUid)
            RebuildGridCuts(gridUid, ignore);
    }

    /// <summary>Scans <see cref="CECableCutComponent"/> entities on a grid into a set of severed edges.</summary>
    private HashSet<CECableCut> BuildGridCutSet(EntityUid gridUid, EntityUid? ignore = null)
    {
        var cuts = new HashSet<CECableCut>();

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return cuts;

        var query = AllEntityQuery<CECableCutComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out _, out var xform))
        {
            if (ent == ignore || xform.GridUid != gridUid || !xform.Anchored)
                continue;

            var tile = _sharedMapSystem.LocalToTile(gridUid, grid, xform.Coordinates);
            var dir = xform.LocalRotation.GetCardinalDir();

            cuts.Add(new CECableCut(tile, tile + dir.ToIntVec()));
        }

        return cuts;
    }

    private void RebuildGridCuts(EntityUid gridUid, EntityUid? ignore = null)
    {
        var cuts = BuildGridCutSet(gridUid, ignore);
        var gridNetEntity = GetNetEntity(gridUid);

        var query = AllEntityQuery<CEPowerMonitoringCableNetworksComponent>();
        while (query.MoveNext(out var ent, out var entCableNetworks))
        {
            if (!IsGridInConsoleNetwork(ent, gridUid))
                continue;

            entCableNetworks.Cuts[gridNetEntity] = cuts;
            Dirty(ent, entCableNetworks);
        }
    }

    private void OnDeviceMapInit(EntityUid uid, CEPowerMonitoringDeviceComponent component, MapInitEvent args)
    {
        if (!Transform(uid).Anchored)
            return;

        RegisterDevice(uid, component);

        if (component.IsCollectionMasterOrChild)
            AssignEntityAsCollectionMaster(uid, component);
    }

    private void OnDeviceAnchorChanged(EntityUid uid, CEPowerMonitoringDeviceComponent component, AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            UnregisterDevice(uid);
            return;
        }

        RegisterDevice(uid, component);

        if (component.IsCollectionMasterOrChild)
            AssignEntityAsCollectionMaster(uid, component);
    }

    private void OnDeviceShutdown(EntityUid uid, CEPowerMonitoringDeviceComponent component, ComponentShutdown args)
    {
        UnregisterDevice(uid);
    }

    private void RegisterDevice(EntityUid uid, CEPowerMonitoringDeviceComponent component)
    {
        var xform = Transform(uid);

        if (xform.GridUid is not { } gridUid)
            return;

        var netEntity = GetNetEntity(uid);
        var name = MetaData(uid).EntityName;
        var coords = GetNetCoordinates(xform.Coordinates);

        var query = AllEntityQuery<CEPowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out _))
        {
            if (!IsGridInConsoleNetwork(ent, gridUid))
                continue;

            entConsole.PowerMonitoringDeviceMetaData[netEntity] =
                new PowerMonitoringDeviceMetaData(name, coords, component.Group, component.SpritePath, component.SpriteState);

            Dirty(ent, entConsole);
        }
    }

    private void UnregisterDevice(EntityUid uid)
    {
        var netEntity = GetNetEntity(uid);

        var query = AllEntityQuery<CEPowerMonitoringConsoleComponent>();
        while (query.MoveNext(out var ent, out var entConsole))
        {
            if (entConsole.PowerMonitoringDeviceMetaData.Remove(netEntity))
                Dirty(ent, entConsole);
        }
    }

    private void OnNodeGroupRebuilt(EntityUid uid, CEPowerMonitoringDeviceComponent component, NodeGroupsRebuilt args)
    {
        if (component.IsCollectionMasterOrChild)
            AssignEntityAsCollectionMaster(uid, component);

        var query = AllEntityQuery<CEPowerMonitoringConsoleComponent, CEPowerMonitoringCableNetworksComponent>();
        while (query.MoveNext(out var _, out var entConsole, out var entCableNetworks))
        {
            if (entConsole.Focus == uid)
                entCableNetworks.FocusChunks.Clear();
        }
    }

    private void OnPowerGridCheckStarted(ref GameRuleStartedEvent ev)
    {
        if (!TryComp<PowerGridCheckRuleComponent>(ev.RuleEntity, out var rule))
            return;

        var query = AllEntityQuery<CEPowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var console, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station == rule.AffectedStation)
            {
                console.Flags |= CEPowerMonitoringFlags.PowerNetAbnormalities;
                Dirty(uid, console);
            }
        }
    }

    private void OnPowerGridCheckEnded(ref GameRuleEndedEvent ev)
    {
        if (!TryComp<PowerGridCheckRuleComponent>(ev.RuleEntity, out var rule))
            return;

        var query = AllEntityQuery<CEPowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var console, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station == rule.AffectedStation)
            {
                console.Flags &= ~CEPowerMonitoringFlags.PowerNetAbnormalities;
                Dirty(uid, console);
            }
        }
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer < UpdateTime)
            return;

        _updateTimer -= UpdateTime;

        var query = AllEntityQuery<CEPowerMonitoringConsoleComponent>();
        while (query.MoveNext(out var ent, out var console))
        {
            if (!_userInterfaceSystem.IsUiOpen(ent, CEPowerMonitoringConsoleUiKey.Key))
                continue;

            UpdateUIState(ent, console);
        }
    }

    private void UpdateUIState(EntityUid uid, CEPowerMonitoringConsoleComponent component)
    {
        var consoleXform = Transform(uid);
        var networkGrids = GetNetworkGrids(uid, consoleXform);

        if (networkGrids.Count == 0)
            return;

        // Every grid must have a NavMapComponent so the client can draw its schematic
        foreach (var grid in networkGrids)
            EnsureComp<NavMapComponent>(grid);

        var totalSources = 0d;
        var totalBatteryUsage = 0d;
        var totalLoads = 0d;
        var allEntries = new List<PowerMonitoringConsoleEntry>();
        var sourcesForFocus = new List<PowerMonitoringConsoleEntry>();
        var loadsForFocus = new List<PowerMonitoringConsoleEntry>();
        var flags = component.Flags;

        component.Flags &= ~(CEPowerMonitoringFlags.RoguePowerConsumer | CEPowerMonitoringFlags.EnergyLeak);

        var powerConsumerQuery = AllEntityQuery<PowerConsumerComponent, TransformComponent>();
        while (powerConsumerQuery.MoveNext(out var ent, out var powerConsumer, out var xform))
        {
            if (!xform.Anchored || xform.GridUid == null || !networkGrids.Contains(xform.GridUid.Value))
                continue;

            if (TryComp<CEPowerMonitoringDeviceComponent>(ent, out _))
                continue;

            if (powerConsumer.ReceivedPower >= RoguePowerConsumerThreshold)
                component.Flags |= CEPowerMonitoringFlags.RoguePowerConsumer;

            totalLoads += powerConsumer.DrawRate;
        }

        // Energy leaks (CE): flag a warning and record locations so the console can blip them on the map.
        var previousLeaks = new Dictionary<NetEntity, NetCoordinates>(component.EnergyLeaks);
        component.EnergyLeaks.Clear();

        var energyLeakQuery = AllEntityQuery<CEEnergyLeakComponent, TransformComponent>();
        while (energyLeakQuery.MoveNext(out var ent, out var leak, out var xform))
        {
            if (!xform.Anchored || xform.GridUid == null || !networkGrids.Contains(xform.GridUid.Value))
                continue;

            if (leak.CurrentLeak <= EnergyLeakWarningThreshold)
                continue;

            component.Flags |= CEPowerMonitoringFlags.EnergyLeak;
            component.EnergyLeaks[GetNetEntity(ent)] = GetNetCoordinates(xform.Coordinates);
        }

        var leaksChanged = previousLeaks.Count != component.EnergyLeaks.Count ||
                           previousLeaks.Any(kv => !component.EnergyLeaks.TryGetValue(kv.Key, out var c) || !c.Equals(kv.Value));

        if (component.Flags != flags || leaksChanged)
            Dirty(uid, component);

        var powerMonitoringDeviceQuery = AllEntityQuery<CEPowerMonitoringDeviceComponent, TransformComponent>();
        while (powerMonitoringDeviceQuery.MoveNext(out var ent, out var device, out var xform))
        {
            // Ignore joined, non-master entities - the master carries their totals.
            if (device.IsCollectionMasterOrChild && !device.IsCollectionMaster)
                continue;

            if (!xform.Anchored || xform.GridUid == null || !networkGrids.Contains(xform.GridUid.Value))
                continue;

            // Safety net: guarantee every visible device has meta data so its list row renders,
            // even if the add-event race left the dict without it.
            var netEnt = GetNetEntity(ent);
            if (!component.PowerMonitoringDeviceMetaData.ContainsKey(netEnt))
            {
                component.PowerMonitoringDeviceMetaData[netEnt] = new PowerMonitoringDeviceMetaData(
                    MetaData(ent).EntityName, GetNetCoordinates(xform.Coordinates), device.Group, device.SpritePath, device.SpriteState);
                Dirty(uid, component);
            }

            var powerStats = GetPowerStats(ent, device);

            totalSources += powerStats.PowerSupplied;
            totalLoads += powerStats.PowerUsage;
            totalBatteryUsage += powerStats.BatteryUsage;

            if (device.Group != component.FocusGroup)
                continue;

            allEntries.Add(new PowerMonitoringConsoleEntry(netEnt, device.Group, powerStats.PowerValue, powerStats.BatteryLevel));
        }

        if (component.Focus != null)
        {
            if (TryComp<NodeContainerComponent>(component.Focus, out var nodeContainer) &&
                TryComp<CEPowerMonitoringDeviceComponent>(component.Focus, out var device))
            {
                if (nodeContainer.Nodes.TryGetValue(device.SourceNode, out var sourceNode))
                    GetSourcesForNode(component.Focus.Value, sourceNode, out sourcesForFocus);

                var loadNodeName = device.LoadNode;

                if (device.LoadNodes != null)
                {
                    var foundNode = nodeContainer.Nodes.FirstOrNull(x => x.Value is CableDeviceNode && (x.Value as CableDeviceNode)?.Enabled == true);

                    if (foundNode != null)
                        loadNodeName = foundNode.Value.Key;
                }

                if (nodeContainer.Nodes.TryGetValue(loadNodeName, out var loadNode))
                    GetLoadsForNode(component.Focus.Value, loadNode, out loadsForFocus);

                if (TryComp<CEPowerMonitoringCableNetworksComponent>(uid, out var cableNetworks) &&
                    cableNetworks.FocusChunks.Count == 0)
                {
                    var reachableEntities = new List<EntityUid>();

                    if (sourceNode?.NodeGroup != null)
                    {
                        foreach (var node in sourceNode.NodeGroup.Nodes)
                            reachableEntities.Add(node.Owner);
                    }

                    if (loadNode?.NodeGroup != null)
                    {
                        foreach (var node in loadNode.NodeGroup.Nodes)
                            reachableEntities.Add(node.Owner);
                    }

                    UpdateFocusNetwork(uid, cableNetworks, networkGrids, reachableEntities);
                }
            }
        }

        _userInterfaceSystem.SetUiState(uid,
            CEPowerMonitoringConsoleUiKey.Key,
            new CEPowerMonitoringConsoleBoundInterfaceState(
                totalSources,
                totalBatteryUsage,
                totalLoads,
                allEntries.ToArray(),
                sourcesForFocus.ToArray(),
                loadsForFocus.ToArray()));
    }

    private PowerStats GetPowerStats(EntityUid uid, CEPowerMonitoringDeviceComponent device)
    {
        var stats = new PowerStats();

        if (device.Group == PowerMonitoringConsoleGroup.Generator)
        {
            if (TryComp<PowerSupplierComponent>(uid, out var supplier))
            {
                stats.PowerValue = supplier.CurrentSupply;
                stats.PowerSupplied += stats.PowerValue;
            }
            else if (TryComp<BatteryDischargerComponent>(uid, out _) &&
                     TryComp<PowerNetworkBatteryComponent>(uid, out var battery))
            {
                stats.PowerValue = battery.NetworkBattery.CurrentSupply;
                stats.PowerSupplied += stats.PowerValue;
                stats.BatteryLevel = GetBatteryLevel(uid);
            }
        }
        else if (device.Group is PowerMonitoringConsoleGroup.SMES
                 or PowerMonitoringConsoleGroup.Substation
                 or PowerMonitoringConsoleGroup.APC)
        {
            if (TryComp<PowerNetworkBatteryComponent>(uid, out var battery))
            {
                stats.BatteryLevel = GetBatteryLevel(uid);
                stats.PowerValue = battery.CurrentSupply;
                stats.PowerUsage += Math.Max(battery.CurrentReceiving - battery.CurrentSupply, 0d);
                stats.BatteryUsage += Math.Max(battery.CurrentSupply - battery.CurrentReceiving, 0d);

                if (device.Group == PowerMonitoringConsoleGroup.APC && battery.Enabled)
                    stats.PowerUsage += battery.NetworkBattery.LoadingNetworkDemand;
            }
        }

        // Master devices add the power values of every entity they represent.
        if (device.IsCollectionMasterOrChild && device.IsCollectionMaster)
        {
            foreach (var (child, childDevice) in device.ChildDevices)
            {
                if (child == uid)
                    continue;

                // Safeguard against infinite recursion.
                if (childDevice.IsCollectionMaster && childDevice.ChildDevices.ContainsKey(uid))
                    continue;

                var childResult = GetPowerStats(child, childDevice);

                stats.PowerValue += childResult.PowerValue;
                stats.PowerSupplied += childResult.PowerSupplied;
                stats.PowerUsage += childResult.PowerUsage;
                stats.BatteryUsage += childResult.BatteryUsage;
            }
        }

        return stats;
    }

    private float? GetBatteryLevel(EntityUid uid)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery))
            return null;

        var effectiveMax = battery.MaxCharge;
        if (effectiveMax == 0)
            effectiveMax = 1;

        return _battery.GetCharge((uid, battery)) / effectiveMax;
    }

    private void GetSourcesForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> sources)
    {
        sources = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return;

        var indexedSources = new Dictionary<EntityUid, PowerMonitoringConsoleEntry>();
        var currentSupply = 0f;
        var currentDemand = 0f;

        foreach (var powerSupplier in netQ.Suppliers)
        {
            var ent = powerSupplier.Owner;

            if (uid == ent)
                continue;

            currentSupply += powerSupplier.CurrentSupply;

            if (TryComp<CEPowerMonitoringDeviceComponent>(ent, out var entDevice))
            {
                // Combine entities represented by a master into a single entry.
                if (entDevice.IsCollectionMasterOrChild && !entDevice.IsCollectionMaster)
                    ent = entDevice.CollectionMaster;

                if (indexedSources.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += powerSupplier.CurrentSupply;
                    indexedSources[ent] = entry;
                    continue;
                }

                indexedSources.Add(ent, new PowerMonitoringConsoleEntry(GetNetEntity(ent), entDevice.Group, powerSupplier.CurrentSupply, GetBatteryLevel(ent)));
            }
        }

        foreach (var batteryDischarger in netQ.Dischargers)
        {
            var ent = batteryDischarger.Owner;

            if (uid == ent)
                continue;

            if (!TryComp<PowerNetworkBatteryComponent>(ent, out var entBattery))
                continue;

            currentSupply += entBattery.CurrentSupply;

            if (TryComp<CEPowerMonitoringDeviceComponent>(ent, out var entDevice))
            {
                // Combine entities represented by a master into a single entry.
                if (entDevice.IsCollectionMasterOrChild && !entDevice.IsCollectionMaster)
                    ent = entDevice.CollectionMaster;

                if (indexedSources.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += entBattery.CurrentSupply;
                    indexedSources[ent] = entry;
                    continue;
                }

                indexedSources.Add(ent, new PowerMonitoringConsoleEntry(GetNetEntity(ent), entDevice.Group, entBattery.CurrentSupply, GetBatteryLevel(ent)));
            }
        }

        sources = indexedSources.Values.ToList();

        foreach (var powerConsumer in netQ.Consumers)
            currentDemand += powerConsumer.ReceivedPower;

        foreach (var batteryCharger in netQ.Chargers)
        {
            var ent = batteryCharger.Owner;

            if (!TryComp<PowerNetworkBatteryComponent>(ent, out var entBattery))
                continue;

            currentDemand += entBattery.CurrentReceiving;
        }

        if (MathHelper.CloseTo(currentDemand, 0) || MathHelper.CloseTo(currentSupply, 0))
            return;

        if (!TryComp<PowerNetworkBatteryComponent>(uid, out var battery))
            return;

        var powerUsage = battery.CurrentReceiving;

        if (TryComp<CEPowerMonitoringDeviceComponent>(uid, out var device) && device.IsCollectionMaster)
        {
            foreach (var (child, _) in device.ChildDevices)
            {
                if (TryComp<PowerNetworkBatteryComponent>(child, out var childBattery))
                    powerUsage += childBattery.CurrentReceiving;
            }
        }

        var powerFraction = Math.Min(powerUsage / currentSupply, 1f) * Math.Min(currentSupply / currentDemand, 1f);

        for (var i = 0; i < sources.Count; i++)
        {
            var entry = sources[i];
            sources[i] = new PowerMonitoringConsoleEntry(entry.NetEntity, entry.Group, entry.PowerValue * powerFraction, entry.BatteryLevel);
        }
    }

    private void GetLoadsForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> loads)
    {
        loads = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return;

        var indexedLoads = new Dictionary<EntityUid, PowerMonitoringConsoleEntry>();
        var currentDemand = 0f;

        foreach (var powerConsumer in netQ.Consumers)
        {
            var ent = powerConsumer.Owner;

            if (uid == ent)
                continue;

            currentDemand += powerConsumer.ReceivedPower;

            if (TryComp<CEPowerMonitoringDeviceComponent>(ent, out var entDevice))
            {
                // Combine entities represented by a master into a single entry.
                if (entDevice.IsCollectionMasterOrChild && !entDevice.IsCollectionMaster)
                    ent = entDevice.CollectionMaster;

                if (indexedLoads.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += powerConsumer.ReceivedPower;
                    indexedLoads[ent] = entry;
                    continue;
                }

                indexedLoads.Add(ent, new PowerMonitoringConsoleEntry(GetNetEntity(ent), entDevice.Group, powerConsumer.ReceivedPower, GetBatteryLevel(ent)));
            }
        }

        foreach (var batteryCharger in netQ.Chargers)
        {
            var ent = batteryCharger.Owner;

            if (uid == ent)
                continue;

            if (!TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
                continue;

            currentDemand += battery.CurrentReceiving;

            if (TryComp<CEPowerMonitoringDeviceComponent>(ent, out var entDevice))
            {
                // Combine entities represented by a master into a single entry.
                if (entDevice.IsCollectionMasterOrChild && !entDevice.IsCollectionMaster)
                    ent = entDevice.CollectionMaster;

                if (indexedLoads.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += battery.CurrentReceiving;
                    indexedLoads[ent] = entry;
                    continue;
                }

                indexedLoads.Add(ent, new PowerMonitoringConsoleEntry(GetNetEntity(ent), entDevice.Group, battery.CurrentReceiving, GetBatteryLevel(ent)));
            }
        }

        loads = indexedLoads.Values.ToList();

        if (MathHelper.CloseTo(currentDemand, 0))
            return;

        var supplying = 0f;

        if (TryComp<PowerNetworkBatteryComponent>(uid, out var entBattery))
            supplying = entBattery.CurrentSupply;
        else if (TryComp<PowerSupplierComponent>(uid, out var entSupplier))
            supplying = entSupplier.CurrentSupply;

        if (TryComp<CEPowerMonitoringDeviceComponent>(uid, out var device) && device.IsCollectionMaster)
        {
            foreach (var (child, _) in device.ChildDevices)
            {
                if (TryComp<PowerNetworkBatteryComponent>(child, out var childBattery))
                    supplying += childBattery.CurrentSupply;
                else if (TryComp<PowerSupplierComponent>(child, out var childSupplier))
                    supplying += childSupplier.CurrentSupply;
            }
        }

        var powerFraction = Math.Min(supplying / currentDemand, 1f);

        for (var i = 0; i < loads.Count; i++)
        {
            var entry = loads[i];
            loads[i] = new PowerMonitoringConsoleEntry(entry.NetEntity, entry.Group, entry.PowerValue * powerFraction, entry.BatteryLevel);
        }
    }

    // Designates a supplied entity as a 'collection master'. Other entities that share its collection
    // name and are attached on the same load network are represented by it as a single console entry.
    private void AssignEntityAsCollectionMaster(
        EntityUid uid,
        CEPowerMonitoringDeviceComponent? device = null,
        TransformComponent? xform = null,
        NodeContainerComponent? nodeContainer = null)
    {
        if (!Resolve(uid, ref device, ref nodeContainer, ref xform, false))
            return;

        var nodeName = device.SourceNode == string.Empty ? device.LoadNode : device.SourceNode;

        if (!nodeContainer.Nodes.TryGetValue(nodeName, out var node) || node.ReachableNodes.Count == 0)
        {
            // Not attached to a network - hand mastership to a child if we have one, then stand alone.
            if (device.ChildDevices.TryFirstOrNull(out var kvp))
            {
                var newMaster = kvp.Value.Key;
                var newMasterDevice = kvp.Value.Value;

                newMasterDevice.CollectionMaster = newMaster;
                newMasterDevice.ChildDevices.Clear();

                foreach (var (child, childDevice) in device.ChildDevices)
                {
                    newMasterDevice.ChildDevices.Add(child, childDevice);
                    childDevice.CollectionMaster = newMaster;
                    UpdateCollectionChildMetaData(child, newMaster);
                }

                UpdateCollectionMasterMetaData(newMaster, newMasterDevice.ChildDevices.Count);
            }

            device.CollectionMaster = uid;
            device.ChildDevices.Clear();
            UpdateCollectionMasterMetaData(uid, 0);

            return;
        }

        // Keep an existing valid master.
        if (!device.IsCollectionMaster &&
            device.CollectionMaster.IsValid() &&
            TryComp<NodeContainerComponent>(device.CollectionMaster, out var masterNodeContainer) &&
            DevicesHaveMatchingNodes(nodeContainer, masterNodeContainer))
            return;

        // Otherwise make this the master and gather matching children.
        device.CollectionMaster = uid;
        device.ChildDevices.Clear();

        var query = AllEntityQuery<CEPowerMonitoringDeviceComponent, TransformComponent, NodeContainerComponent>();
        while (query.MoveNext(out var ent, out var entDevice, out var entXform, out var entNodeContainer))
        {
            if (entDevice.CollectionName != device.CollectionName)
                continue;

            if (ent == uid)
                continue;

            if (entXform.GridUid != xform.GridUid)
                continue;

            if (!DevicesHaveMatchingNodes(nodeContainer, entNodeContainer))
                continue;

            device.ChildDevices.Add(ent, entDevice);
            entDevice.CollectionMaster = uid;
            UpdateCollectionChildMetaData(ent, uid);
        }

        UpdateCollectionMasterMetaData(uid, device.ChildDevices.Count);
    }

    private bool DevicesHaveMatchingNodes(NodeContainerComponent nodeContainerA, NodeContainerComponent nodeContainerB)
    {
        foreach (var (key, nodeA) in nodeContainerA.Nodes)
        {
            if (!nodeContainerB.Nodes.TryGetValue(key, out var nodeB))
                return false;

            if (nodeA.NodeGroup != nodeB.NodeGroup)
                return false;
        }

        return true;
    }

    private void UpdateCollectionChildMetaData(EntityUid child, EntityUid master)
    {
        var netEntity = GetNetEntity(child);
        var xform = Transform(child);

        var query = AllEntityQuery<CEPowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out _))
        {
            if (xform.GridUid == null || !IsGridInConsoleNetwork(ent, xform.GridUid.Value))
                continue;

            if (!entConsole.PowerMonitoringDeviceMetaData.TryGetValue(netEntity, out var metaData))
                continue;

            metaData.CollectionMaster = GetNetEntity(master);
            entConsole.PowerMonitoringDeviceMetaData[netEntity] = metaData;
            Dirty(ent, entConsole);
        }
    }

    private void UpdateCollectionMasterMetaData(EntityUid master, int childCount)
    {
        var netEntity = GetNetEntity(master);
        var xform = Transform(master);

        var query = AllEntityQuery<CEPowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out _))
        {
            if (xform.GridUid == null || !IsGridInConsoleNetwork(ent, xform.GridUid.Value))
                continue;

            if (!entConsole.PowerMonitoringDeviceMetaData.TryGetValue(netEntity, out var metaData))
                continue;

            if (childCount > 0)
            {
                var name = MetaData(master).EntityPrototype?.Name ?? MetaData(master).EntityName;
                metaData.EntityName = Loc.GetString("ce-power-monitoring-window-object-array", ("name", name), ("count", childCount + 1));
            }
            else
            {
                metaData.EntityName = MetaData(master).EntityName;
            }

            metaData.CollectionMaster = null;
            entConsole.PowerMonitoringDeviceMetaData[netEntity] = metaData;
            Dirty(ent, entConsole);
        }
    }

    private Dictionary<Vector2i, PowerCableChunk> RefreshPowerCableGrid(EntityUid gridUid, MapGridComponent grid)
    {
        var allChunks = new Dictionary<Vector2i, PowerCableChunk>();
        _gridPowerCableChunks[gridUid] = allChunks;

        var query = AllEntityQuery<CableComponent, TransformComponent>();
        while (query.MoveNext(out _, out var cable, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            var tile = _sharedMapSystem.GetTileRef(gridUid, grid, entXform.Coordinates);
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.GridIndices, ChunkSize);

            if (!allChunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new PowerCableChunk(chunkOrigin);
                allChunks[chunkOrigin] = chunk;
            }

            var relative = SharedMapSystem.GetChunkRelative(tile.GridIndices, ChunkSize);
            chunk.PowerCableData[(int) cable.CableType] |= GetFlag(relative);
        }

        return allChunks;
    }

    private void UpdateFocusNetwork(EntityUid uid, CEPowerMonitoringCableNetworksComponent component, List<EntityUid> networkGrids, List<EntityUid> nodeList)
    {
        component.FocusChunks.Clear();

        foreach (var ent in nodeList)
        {
            var xform = Transform(ent);

            if (xform.GridUid == null || !networkGrids.Contains(xform.GridUid.Value) ||
                !TryComp<MapGridComponent>(xform.GridUid, out var grid))
                continue;

            var gridNetEntity = GetNetEntity(xform.GridUid.Value);

            if (!component.FocusChunks.TryGetValue(gridNetEntity, out var chunks))
            {
                chunks = new();
                component.FocusChunks[gridNetEntity] = chunks;
            }

            var tile = _sharedMapSystem.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.GridIndices, ChunkSize);

            if (!chunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new PowerCableChunk(chunkOrigin);
                chunks[chunkOrigin] = chunk;
            }

            var relative = SharedMapSystem.GetChunkRelative(tile.GridIndices, ChunkSize);

            if (TryComp<CableComponent>(ent, out var cable))
                chunk.PowerCableData[(int) cable.CableType] |= GetFlag(relative);
        }

        Dirty(uid, component);
    }

    private void RefreshPowerMonitoringConsole(EntityUid uid, CEPowerMonitoringConsoleComponent component)
    {
        component.Focus = null;
        component.FocusGroup = PowerMonitoringConsoleGroup.Generator;
        component.PowerMonitoringDeviceMetaData.Clear();
        component.Flags = 0;

        var networkGrids = GetNetworkGrids(uid);

        if (networkGrids.Count == 0)
            return;

        var query = AllEntityQuery<CEPowerMonitoringDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entDevice, out var entXform))
        {
            if (entXform.GridUid == null || !networkGrids.Contains(entXform.GridUid.Value))
                continue;

            var netEntity = GetNetEntity(ent);
            var name = MetaData(ent).EntityName;
            var netCoords = GetNetCoordinates(entXform.Coordinates);

            var metaData = new PowerMonitoringDeviceMetaData(name, netCoords, entDevice.Group, entDevice.SpritePath, entDevice.SpriteState);

            if (entDevice.IsCollectionMasterOrChild)
            {
                if (!entDevice.IsCollectionMaster)
                {
                    metaData.CollectionMaster = GetNetEntity(entDevice.CollectionMaster);
                }
                else if (entDevice.ChildDevices.Count > 0)
                {
                    name = MetaData(ent).EntityPrototype?.Name ?? MetaData(ent).EntityName;
                    metaData.EntityName = Loc.GetString("ce-power-monitoring-window-object-array", ("name", name), ("count", entDevice.ChildDevices.Count + 1));
                }
            }

            component.PowerMonitoringDeviceMetaData[netEntity] = metaData;
        }

        Dirty(uid, component);
    }

    private void RefreshPowerMonitoringCableNetworks(EntityUid uid, CEPowerMonitoringCableNetworksComponent component)
    {
        component.AllChunks.Clear();
        component.FocusChunks.Clear();
        component.Cuts.Clear();
        component.VerticalPipes.Clear();

        foreach (var grid in GetNetworkGrids(uid))
        {
            if (!TryComp<MapGridComponent>(grid, out var map))
                continue;

            if (!_gridPowerCableChunks.TryGetValue(grid, out var allChunks))
                allChunks = RefreshPowerCableGrid(grid, map);

            var gridNet = GetNetEntity(grid);
            component.AllChunks[gridNet] = allChunks;
            component.Cuts[gridNet] = BuildGridCutSet(grid);
            component.VerticalPipes[gridNet] = BuildGridVerticalPipeList(grid);
        }

        Dirty(uid, component);
    }

    private struct PowerStats
    {
        public double PowerValue { get; set; }
        public double PowerSupplied { get; set; }
        public double PowerUsage { get; set; }
        public double BatteryUsage { get; set; }
        public float? BatteryLevel { get; set; }
    }
}
