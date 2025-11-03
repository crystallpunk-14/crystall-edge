using Content.Client._CE.Power.Components;
using Content.Shared.Wires;
using Robust.Client.GameObjects;

namespace Content.Client._CE.Power;

public sealed class CEPipeVisualizerSystem : VisualizerSystem<CEPipeVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, CEPipeVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<WireVisDirFlags>(uid, WireVisVisuals.ConnectedMask, out var mask, args.Component))
            mask = WireVisDirFlags.None;

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, $"{component.StatePrefix}{(int)mask}");
        if (component.ExtraLayerPrefix != null)
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), 1, $"{component.ExtraLayerPrefix}{(int)mask}");
    }
}
