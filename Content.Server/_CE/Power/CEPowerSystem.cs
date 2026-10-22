using Content.Server._CE.Power.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Nodes;
using Content.Server.Radiation.Systems;
using Content.Shared._CE.Power;
using Content.Shared._CE.Power.Components;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;

namespace Content.Server._CE.Power;

public sealed partial class CEPowerSystem : CESharedPowerSystem
{
    [Dependency] private RadiationSystem _radiation = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private NodeGroupSystem _nodeGroup = default!;

    [Dependency] private EntityQuery<NodeContainerComponent> _nodeQuery = default!;
    [Dependency] private EntityQuery<ApcPowerReceiverComponent> _receiverQuery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateChargers(frameTime);
    }

    /// <summary>
    /// Powered APC receivers (machines) or anything whose own power net has supply (pipes, batteries).
    /// </summary>
    public bool IsPowered(EntityUid uid)
    {
        if (_receiverQuery.TryComp(uid, out var receiver) && receiver.Powered)
            return true;

        if (!_nodeQuery.TryComp(uid, out var nodeContainer))
            return false;

        foreach (var node in nodeContainer.Nodes.Values)
        {
            if (node.NodeGroup is IBasePowerNet net && IsPowered(net))
                return true;
        }

        return false;
    }

    /// <summary>
    /// A network counts as powered when it has at least one enabled source (generator or discharging battery).
    /// Written by the power solver every tick.
    /// </summary>
    public static bool IsPowered(IBasePowerNet net) => net.NetworkNode.LastCombinedMaxSupply > 0;

    public void ToggleConnector(Entity<NodeContainerComponent> connector, bool status)
    {
        foreach (var node in connector.Comp.Nodes.Values)
        {
            if (node is CEConnectorCenterNode cableNode)
            {
                cableNode.Active = status;
                _nodeGroup.QueueReflood(node);
            }
        }

        _appearance.SetData(connector, CEToggleableCableVisuals.Enabled, status);
    }

    [SubscribeLocalEvent]
    private void OnActivateInWorld(Entity<CEToggleableConnectorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (UseDelay.IsDelayed(ent.Owner))
            return;

        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        var newState = !ent.Comp.Active;
        ent.Comp.Active = newState;
        ToggleConnector((ent, nodeContainer), newState);

        UseDelay.TryResetDelay(ent);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CEEnergyLeakComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        var enabled = args.ReceivedPower > 0;

        PointLight.SetEnabled(ent, enabled);

        if (TryComp<RadiationSourceComponent>(ent, out var radComp))
        {
            _radiation.SetSourceEnabled((ent.Owner, radComp), enabled);
            _radiation.SetIntensity((ent.Owner, radComp), args.ReceivedPower * ent.Comp.LeakPercentage);
        }

        ent.Comp.CurrentLeak = args.ReceivedPower * ent.Comp.LeakPercentage;
        Dirty(ent);
    }
}
