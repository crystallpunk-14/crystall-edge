using Content.Client.Light.Components;
using Content.Client.Light.EntitySystems;
using Content.Shared._CE.Power;
using Content.Shared._CE.Power.Components;
using Robust.Client.GameObjects;

namespace Content.Client._CE.Power;

public sealed class CEClientPowerSystem : VisualizerSystem<CEEnergyLeakComponent>
{
    [Dependency] private readonly LightBehaviorSystem _light = default!;

    protected override void OnAppearanceChange(EntityUid uid, CEEnergyLeakComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!AppearanceSystem.TryGetData<bool>(uid, CEPowerConsumerVisuals.Active, out var enabled))
            return;

        if (!TryComp<LightBehaviourComponent>(uid, out var beh))
            return;

        if (component.CurrentLeak > 0)
        {
            _light.StartLightBehaviour((uid, beh));
        }
        else
        {
            _light.StopLightBehaviour((uid, beh));
        }
    }
}
