using Content.Shared._CE.InfusionAltar;
using Robust.Client.GameObjects;

namespace Content.Client._CE.InfusionAltar;

public sealed class CEInfusionAltarDangerVisualsSystem : VisualizerSystem<CEInfusionAltarDangerVisualsComponent>
{
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    protected override void OnAppearanceChange(EntityUid uid, CEInfusionAltarDangerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<PointLightComponent>(uid, out var light))
            return;

        component.BaseColor ??= light.Color;

        if (!AppearanceSystem.TryGetData<CEInfusionAltarDangerLevel>(uid, CEInfusionAltarDangerVisuals.Level, out var level, args.Component))
            level = CEInfusionAltarDangerLevel.Calm;

        var color = level switch
        {
            CEInfusionAltarDangerLevel.Critical => component.CriticalColor,
            CEInfusionAltarDangerLevel.Unstable => component.UnstableColor,
            _ => component.BaseColor.Value,
        };

        _light.SetColor(uid, color, light);
    }
}
