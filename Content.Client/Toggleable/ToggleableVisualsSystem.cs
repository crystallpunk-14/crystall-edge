using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Toggleable;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.Toggleable;

/// <summary>
/// Implements the behavior of <see cref="ToggleableVisualsComponent"/> by reacting to
/// <see cref="AppearanceChangeEvent"/>, for the sprite directly; <see cref="OnGetHeldVisuals"/> for the
/// in-hand visuals; and <see cref="OnGetEquipmentVisuals"/> for the clothing visuals.
/// </summary>
/// <see cref="ToggleableVisualsComponent"/>
public sealed partial class ToggleableVisualsSystem : VisualizerSystem<ToggleableVisualsComponent>
{
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    // CrystallEdge: needed to check whether a wielded-prefixed in-hand state exists before using it
    [Dependency] private IResourceCache _resCache = default!;
    // CrystallEdge end

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleableVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals,
            after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<ToggleableVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals,
            after: [typeof(ClientClothingSystem)]);
    }

    protected override void OnAppearanceChange(EntityUid uid,
        ToggleableVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<bool>(uid, ToggleableVisuals.Enabled, out var enabled, args.Component))
            return;

        var modulateColor =
            AppearanceSystem.TryGetData<Color>(uid, ToggleableVisuals.Color, out var color, args.Component);

        // Update the item's sprite
        if (args.Sprite != null && component.SpriteLayer != null &&
            SpriteSystem.LayerMapTryGet((uid, args.Sprite), component.SpriteLayer, out var layer, false))
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, enabled);
            if (modulateColor)
                SpriteSystem.LayerSetColor((uid, args.Sprite), component.SpriteLayer, color);
        }

        // If there's a `ItemTogglePointLightComponent` that says to apply the color to attached lights, do so.
        if (TryComp<ItemTogglePointLightComponent>(uid, out var toggleLights) &&
            TryComp(uid, out PointLightComponent? light))
        {
            DebugTools.Assert(!light.NetSyncEnabled,
                $"{typeof(ItemTogglePointLightComponent)} requires point lights without net-sync");
            _pointLight.SetEnabled(uid, enabled, light);
            if (modulateColor && toggleLights.ToggleableVisualsColorModulatesLights)
            {
                _pointLight.SetColor(uid, color, light);
            }
        }

        // update clothing & in-hand visuals.
        _item.VisualsChanged(uid);
    }

    private void OnGetEquipmentVisuals(EntityUid uid,
        ToggleableVisualsComponent component,
        GetEquipmentVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance)
            || !AppearanceSystem.TryGetData<bool>(uid, ToggleableVisuals.Enabled, out var enabled, appearance)
            || !enabled)
            return;

        if (!TryComp(args.Equipee, out InventoryComponent? inventory))
            return;
        List<PrototypeLayerData>? layers = null;

        // attempt to get species specific data
        if (inventory.SpeciesId != null)
            component.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);

        // No species specific data.  Try to default to generic data.
        if (layers == null && !component.ClothingVisuals.TryGetValue(args.Slot, out layers))
            return;

        var modulateColor = AppearanceSystem.TryGetData<Color>(uid, ToggleableVisuals.Color, out var color, appearance);

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? $"{args.Slot}-toggle" : $"{args.Slot}-toggle-{i}";
                i++;
            }

            if (modulateColor)
                layer.Color = color;

            args.Layers.Add((key, layer));
        }
    }

    private void OnGetHeldVisuals(EntityUid uid, ToggleableVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance)
            || !AppearanceSystem.TryGetData<bool>(uid, ToggleableVisuals.Enabled, out var enabled, appearance)
            || !enabled)
            return;

        if (!component.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        var modulateColor = AppearanceSystem.TryGetData<Color>(uid, ToggleableVisuals.Color, out var color, appearance);

        // CrystallEdge: swap in a wielded-prefixed in-hand state (e.g. "wielded-inhand-left-charge") while the
        // item is wielded, so toggle overlays (charge glow, etc.) follow the two-handed grip pose instead of
        // staying stuck on the one-handed sprite.
        var wieldedPrefix = TryComp(uid, out WieldableComponent? wieldable) && wieldable.Wielded
            ? wieldable.WieldedInhandPrefix
            : null;
        // CrystallEdge end

        var i = 0;
        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}-toggle";
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                i++;
            }

            var layerToAdd = layer;

            // CrystallEdge: use the wielded-prefixed state instead, if one is actually defined for this layer
            if (wieldedPrefix != null && TryGetWieldedState(uid, layer, wieldedPrefix, out var wieldedState))
            {
                layerToAdd = CloneWithState(layer, wieldedState);
            }
            // CrystallEdge end

            if (modulateColor)
                layerToAdd.Color = color;

            args.Layers.Add((key, layerToAdd));
        }
    }

    // CrystallEdge: helpers for wielded-prefixed toggle overlay states, see OnGetHeldVisuals above
    private bool TryGetWieldedState(EntityUid uid, PrototypeLayerData layer, string prefix, [NotNullWhen(true)] out string? state)
    {
        state = null;
        if (layer.State == null)
            return false;

        var candidate = $"{prefix}-{layer.State}";

        RSI? rsi = null;
        if (layer.RsiPath != null)
            rsi = _resCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / layer.RsiPath).RSI;
        else if (TryComp(uid, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        if (rsi == null || !rsi.TryGetState(candidate, out _))
            return false;

        state = candidate;
        return true;
    }

    private static PrototypeLayerData CloneWithState(PrototypeLayerData layer, string state)
    {
        return new PrototypeLayerData
        {
            Shader = layer.Shader,
            TexturePath = layer.TexturePath,
            RsiPath = layer.RsiPath,
            State = state,
            Scale = layer.Scale,
            Rotation = layer.Rotation,
            Offset = layer.Offset,
            Visible = layer.Visible,
            Color = layer.Color,
            MapKeys = layer.MapKeys,
            RenderingStrategy = layer.RenderingStrategy,
            CopyToShaderParameters = layer.CopyToShaderParameters,
            Cycle = layer.Cycle,
            Loop = layer.Loop,
        };
    }
    // CrystallEdge end
}
