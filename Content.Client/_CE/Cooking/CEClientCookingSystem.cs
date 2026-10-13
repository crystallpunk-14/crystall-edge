/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Client.DisplacementMap;
using Content.Shared._CE.Cooking;
using Content.Shared._CE.Cooking.Components;
using Content.Shared.DisplacementMap;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;

namespace Content.Client._CE.Cooking;

public sealed partial class CEClientCookingSystem : CESharedCookingSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<CEFoodHolderComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<CEFoodHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<CEFoodHolderComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        //Remove old layers
        foreach (var key in ent.Comp.RevealedLayers)
        {
            _sprite.RemoveLayer((ent, sprite), key);
        }

        ent.Comp.RevealedLayers.Clear();

        if (ent.Comp.FoodData is null)
            return;

        if (!Solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out _, out var solution))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), ent.Comp.TargetLayerMap, out var index, true))
            return;

        var fillLevel = (float)solution.Volume / (float)solution.MaxVolume;
        if (fillLevel > 1)
            fillLevel = 1;

        var closestFillSprite = ContentHelpers.RoundToLevels(fillLevel, 1, ent.Comp.MaxDisplacementFillLevels + 1);

        //Add new layers
        var counter = 0;
        foreach (var layer in ent.Comp.FoodData.Visuals)
        {
            var layerIndex = index + counter;
            var keyCode = $"ce-food-layer-{counter}";
            ent.Comp.RevealedLayers.Add(keyCode);

            _sprite.AddBlankLayer((ent, sprite), layerIndex);
            _sprite.LayerMapSet((ent, sprite), keyCode, layerIndex);
            _sprite.LayerSetData((ent, sprite), layerIndex, layer);

            if (ent.Comp.DisplacementRsiPath is not null)
            {
                var displaceData = new DisplacementData
                {
                    SizeMaps = new Dictionary<int, PrototypeLayerData>
                    {
                        {
                            32, new PrototypeLayerData
                            {
                                RsiPath = ent.Comp.DisplacementRsiPath,
                                State = "fill-" + closestFillSprite,
                            }
                        },
                    }
                };
                if (_displacement.TryAddDisplacement(displaceData,
                        (ent, sprite),
                        layerIndex,
                        keyCode,
                        out var displacementKey))
                {
                    ent.Comp.RevealedLayers.Add(displacementKey);
                }
            }

            counter++;
        }
    }
}
