using Content.Server._CE.Power.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Server.Radiation.Systems;
using Content.Shared._CE.Power;
using Content.Shared._CE.Power.Components;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Content.Shared.Power.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Spawners;

namespace Content.Server._CE.Power;

public sealed class CEPowerSystem : EntitySystem
{
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEEnergyLeakComponent, PowerConsumerReceivedChanged>(OnPowerChanged);
        SubscribeLocalEvent<CEIrradiateOnDestroyComponent, DestructionEventArgs>(OnBatteryDestroyed);
        SubscribeLocalEvent<CEToggleableCableComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnActivateInWorld(Entity<CEToggleableCableComponent> ent, ref ActivateInWorldEvent args)
    {
        if (_useDelay.IsDelayed(ent.Owner))
            return;

        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        var newState = !ent.Comp.Active;
        ent.Comp.Active = newState;
        foreach (var node in nodeContainer.Nodes.Values)
        {
            if (node is CableNode cableNode)
            {
                cableNode.Active = newState;
                _nodeGroup.QueueReflood(node);
            }
        }

        _appearance.SetData(ent, CEToggleableCableVisuals.Enabled, newState);
        _useDelay.TryResetDelay(ent);
    }

    private void OnBatteryDestroyed(Entity<CEIrradiateOnDestroyComponent> ent, ref DestructionEventArgs args)
    {
        if (!TryComp<BatteryComponent>(ent, out var battery))
            return;

        var vfx = SpawnAtPosition(ent.Comp.Proto, Transform(ent).Coordinates);

        var radiation = EnsureComp<RadiationSourceComponent>(vfx);
        radiation.Enabled = true;
        radiation.Intensity = battery.CurrentCharge / ent.Comp.Time.Seconds * ent.Comp.IrradiateCoefficient;

        var timeDespawn = EnsureComp<TimedDespawnComponent>(vfx);
        timeDespawn.Lifetime = ent.Comp.Time.Seconds;
    }

    private void OnPowerChanged(Entity<CEEnergyLeakComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        var enabled = args.ReceivedPower >= args.DrawRate;

        _pointLight.SetEnabled(ent, enabled);

        if (TryComp<RadiationSourceComponent>(ent, out var radComp))
        {
            _radiation.SetSourceEnabled((ent.Owner, radComp), enabled);
            radComp.Intensity = args.ReceivedPower * ent.Comp.LeakPercentage;
        }

        ent.Comp.CurrentLeak = args.ReceivedPower * ent.Comp.LeakPercentage;
        Dirty(ent);
    }
}
