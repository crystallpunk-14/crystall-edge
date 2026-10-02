using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Shared._CE.EnergyScanner;
using Content.Shared._CE.Examine;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Server._CE.EnergyScanner;

/// <summary>
/// Energy scanner glasses: grants <see cref="CEEnergyScannerViewerComponent"/> to the wearer, keeps pipe appearance
/// data for the client overlay up to date and adds power network readings to examine.
/// </summary>
public sealed partial class CEEnergyScannerSystem : EntitySystem
{
    [Dependency] private PowerNetSystem _powerNet = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<CEEnergyScannerClothingComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.EYES) == 0)
            return;

        EnsureComp<CEEnergyScannerViewerComponent>(args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnUnequipped(Entity<CEEnergyScannerClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.EYES) == 0)
            return;

        RemComp<CEEnergyScannerViewerComponent>(args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnExamineAugment(CEExamineAugmentEvent args)
    {
        if (!HasComp<CEEnergyScannerViewerComponent>(args.Examiner) ||
            !TryComp<NodeContainerComponent>(args.Examined, out var nodeContainer) ||
            !_examine.IsInDetailsRange(args.Examiner, args.Examined))
            return;

        var seen = new HashSet<IBasePowerNet>();
        foreach (var node in nodeContainer.Nodes.Values)
        {
            if (node.NodeGroup is not IBasePowerNet net || !seen.Add(net))
                continue;

            args.AddMarkup(GetNetworkMarkup(node.NodeGroupID, net));
        }
    }

    private string GetNetworkMarkup(NodeGroupID groupId, IBasePowerNet net)
    {
        var stats = _powerNet.GetNetworkStatistics(net.NetworkNode);

        var header = Loc.GetString(groupId switch
        {
            NodeGroupID.HVPower => "ce-energy-scanner-network-big",
            NodeGroupID.Apc => "ce-energy-scanner-network-medium",
            _ => "ce-energy-scanner-network-other",
        });

        var status = Loc.GetString(IsPowered(net) ? "ce-energy-scanner-powered" : "ce-energy-scanner-unpowered");

        return Loc.GetString("ce-energy-scanner-statistics",
            ("header", header),
            ("status", status),
            ("supplyc", FormatValue(stats.SupplyCurrent)),
            ("supplyb", FormatValue(stats.SupplyBatteries)),
            ("supplym", FormatValue(stats.SupplyTheoretical)),
            ("consumption", FormatValue(stats.Consumption)),
            ("storagec", FormatValue(stats.InStorageCurrent)),
            ("storagem", FormatValue(stats.InStorageMax)),
            ("storager", stats.InStorageCurrent / Math.Max(stats.InStorageMax, 1f)),
            ("storageoc", FormatValue(stats.OutStorageCurrent)),
            ("storageom", FormatValue(stats.OutStorageMax)),
            ("storageor", stats.OutStorageCurrent / Math.Max(stats.OutStorageMax, 1f)));
    }

    private static string FormatValue(float value) => Math.Round(value).ToString("N0");

    /// <summary>
    /// A network counts as powered when it has at least one enabled source (generator or discharging battery).
    /// Written by the power solver every tick.
    /// </summary>
    private static bool IsPowered(IBasePowerNet net) => net.NetworkNode.LastCombinedMaxSupply > 0;
}
