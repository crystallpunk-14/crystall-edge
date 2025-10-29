using Content.Server.Power.EntitySystems;
using Content.Server.Radiation.Systems;
using Content.Shared._CE.Power.Components;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;

namespace Content.Server._CE.Power;

public sealed class CEPowerSystem : EntitySystem
{
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEEnergyLeakComponent, PowerConsumerReceivedChanged>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<CEEnergyLeakComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        var enabled = args.ReceivedPower > 0;

        _pointLight.SetEnabled(ent, enabled);

        if (TryComp<RadiationSourceComponent>(ent, out var radComp))
        {
            _radiation.SetSourceEnabled((ent.Owner, radComp), enabled);
            radComp.Intensity = args.ReceivedPower * ent.Comp.LeakPercentage;
        }

        ent.Comp.CurrentLeak = args.ReceivedPower * ent.Comp.LeakPercentage;
        Dirty(ent);

        _appearance.SetData(ent, CEEnergyLeakVisuals.Enabled, enabled);
    }
}
