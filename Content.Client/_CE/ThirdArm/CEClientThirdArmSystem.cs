using Content.Client.Items.Systems;
using Content.Shared._CE.ThirdArm;
using Content.Shared._CE.ThirdArm.Components;
using Content.Shared.Clothing;
using Robust.Client.GameObjects;

namespace Content.Client._CE.ThirdArm;

public sealed partial class CEClientThirdArmSystem : CESharedThirdArmSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private ItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEThirdArmComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<CEThirdArmComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals);
    }

    private void OnAppearanceChange(Entity<CEThirdArmComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Remove previously revealed module layers
        foreach (var key in ent.Comp.RevealedLayers)
        {
            _sprite.RemoveLayer((ent, args.Sprite), key);
        }
        ent.Comp.RevealedLayers.Clear();

        Appearance.TryGetData<List<PrototypeLayerData>>(ent, CEThirdArmVisuals.IconLayers, out var layers, args.Component);
        layers ??= new List<PrototypeLayerData>();

        var counter = 0;
        foreach (var layer in layers)
        {
            var keyCode = $"third_arm_module_icon_{counter}";
            var index = _sprite.AddLayer((ent, args.Sprite), layer, null);
            _sprite.LayerMapSet((ent, args.Sprite), keyCode, index);
            ent.Comp.RevealedLayers.Add(keyCode);
            counter++;
        }

        _itemSystem.VisualsChanged(ent);
    }

    private void OnGetEquipmentVisuals(Entity<CEThirdArmComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!Appearance.TryGetData<List<PrototypeLayerData>>(ent, CEThirdArmVisuals.EquippedLayers, out var layers, appearance))
            return;

        var counter = 0;
        foreach (var layer in layers)
        {
            args.Layers.Add(($"third_arm_module_equipped_{counter}", layer));
            counter++;
        }
    }
}
