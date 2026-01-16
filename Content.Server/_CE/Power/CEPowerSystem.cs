using Content.Server._CE.Power.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Server.Radiation.Systems;
using Content.Shared._CE.Power;
using Content.Shared._CE.Power.Components;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;

namespace Content.Server._CE.Power;

public sealed partial class CEPowerSystem : CESharedPowerSystem
{
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEEnergyLeakComponent, PowerConsumerReceivedChanged>(OnPowerChanged);
        SubscribeLocalEvent<CEToggleableConnectorComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateChargers(frameTime);
    }

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

    private void OnActivateInWorld(Entity<CEToggleableConnectorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (UseDelay.IsDelayed(ent.Owner))
            return;

        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        var newState = !ent.Comp.Active;
        ent.Comp.Active = newState;
        ToggleConnector((ent, nodeContainer), newState);

        UseDelay.TryResetDelay(ent);
    }

    private void OnPowerChanged(Entity<CEEnergyLeakComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        var enabled = args.ReceivedPower >= 0;

        PointLight.SetEnabled(ent, enabled);

        if (TryComp<RadiationSourceComponent>(ent, out var radComp))
        {
            _radiation.SetSourceEnabled((ent.Owner, radComp), enabled);
            radComp.Intensity = args.ReceivedPower * ent.Comp.LeakPercentage;
        }

        ent.Comp.CurrentLeak = args.ReceivedPower * ent.Comp.LeakPercentage;
        Dirty(ent);
    }
}
