using Content.Shared._CE.Alchemy;
using Robust.Client.GameObjects;

namespace Content.Client._CE.Alchemy;

/// <summary>
/// Paints the transmutation flash (sprite and light) in the color of the mixture.
/// </summary>
public sealed partial class CEAlchemyTransmuteVFXSystem : VisualizerSystem<CEAlchemyTransmuteVFXComponent>
{
    [Dependency] private SharedPointLightSystem _light = default!;

    protected override void OnAppearanceChange(EntityUid uid, CEAlchemyTransmuteVFXComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!AppearanceSystem.TryGetData<Color>(uid, CEAlchemyTransmuteVisuals.Color, out var color, args.Component))
            return;

        if (args.Sprite != null)
            SpriteSystem.SetColor((uid, args.Sprite), color);

        _light.SetColor(uid, color);
    }
}
