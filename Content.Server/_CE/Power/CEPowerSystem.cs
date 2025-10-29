using Content.Server._CE.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radiation.Systems;
using Content.Shared.Radiation.Components;

namespace Content.Server._CE.Power;

public sealed class CEPowerSystem : EntitySystem
{
    [Dependency] private readonly RadiationSystem _radiation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEEnergyLeakComponent, PowerConsumerReceivedChanged>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<CEEnergyLeakComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        var enabled = args.ReceivedPower > 0;

        if (TryComp<RadiationSourceComponent>(ent, out var radComp))
        {
            _radiation.SetSourceEnabled((ent.Owner, radComp), enabled);
            radComp.Intensity = args.ReceivedPower * ent.Comp.LeakPercentage;
        }
    }
}
