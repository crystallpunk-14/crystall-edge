using Content.Shared._CE.Farming;
using Content.Shared._CE.Farming.Components;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client._CE.Farming;

public sealed class CEClientFarmingSystem : CESharedFarmingSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEPlantVisualsComponent, ComponentInit>(OnPlantVisualInit);
        SubscribeLocalEvent<CEPlantComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);
        SubscribeLocalEvent<CEPlantProducingComponent, AfterAutoHandleStateEvent>(OnProduceAutoHandleState);
    }

    private void OnProduceAutoHandleState(Entity<CEPlantProducingComponent> producing, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<CEPlantVisualsComponent>(producing, out var visuals))
            return;

        if (!PlantQuery.TryComp(producing.Owner, out var plant))
            return;

        UpdateVisuals(new Entity<CEPlantVisualsComponent>(producing, visuals), plant);
    }

    private void OnAutoHandleState(Entity<CEPlantComponent> plant, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<CEPlantVisualsComponent>(plant, out var visuals))
            return;

        UpdateVisuals(new Entity<CEPlantVisualsComponent>(plant, visuals), plant);
    }

    private void OnPlantVisualInit(Entity<CEPlantVisualsComponent> visuals, ref ComponentInit args)
    {
        UpdateVisuals(visuals);
    }

    private void UpdateVisuals(Entity<CEPlantVisualsComponent> visuals, CEPlantComponent? plant = null)
    {
        if (!Resolve(visuals, ref plant, false))
            return;

        PlantProducingQuery.TryComp(visuals.Owner, out var producing);

        if (plant.GrowthLevel < 1) //Growing
        {
            visuals.Comp.SelectedVariation = null;

            var growthState = ContentHelpers.RoundToNearestLevels(plant.GrowthLevel, 1, visuals.Comp.GrowthSteps);
            if (growthState == 0)
                growthState++;

            if (_sprite.LayerMapTryGet(visuals.Owner, PlantVisualLayers.Base, out _, false))
                _sprite.LayerSetRsiState(visuals.Owner,
                    PlantVisualLayers.Base,
                    $"{visuals.Comp.GrowState}{growthState}");

            if (_sprite.LayerMapTryGet(visuals.Owner, PlantVisualLayers.BaseUnshaded, out _, false))
                _sprite.LayerSetRsiState(visuals.Owner,
                    PlantVisualLayers.BaseUnshaded,
                    $"{visuals.Comp.GrowUnshadedState}{growthState}");

            if (producing is not null)
            {
                foreach (var (key, entry) in producing.GatherKeys)
                {
                    if (_sprite.LayerMapTryGet(visuals.Owner, $"{PlantVisualLayers.Produce.ToString()}-{key}", out _, false))
                        _sprite.LayerSetVisible(visuals.Owner, $"{PlantVisualLayers.Produce.ToString()}-{key}", false);
                }
            }
        }
        else //Fully frown
        {
            if (visuals.Comp.SelectedVariation is null)
                visuals.Comp.SelectedVariation = _random.Next(1, visuals.Comp.ReadyVariations + 1);

            if (_sprite.LayerMapTryGet(visuals.Owner, PlantVisualLayers.Base, out _, false))
                _sprite.LayerSetRsiState(visuals.Owner,
                    PlantVisualLayers.Base,
                    $"{visuals.Comp.ReadyState}{visuals.Comp.SelectedVariation}");

            if (_sprite.LayerMapTryGet(visuals.Owner, PlantVisualLayers.BaseUnshaded, out _, false))
                _sprite.LayerSetRsiState(visuals.Owner,
                    PlantVisualLayers.BaseUnshaded,
                    $"{visuals.Comp.ReadyUnshadedState}{visuals.Comp.SelectedVariation}");

            if (producing is not null)
            {
                foreach (var (key, entry) in producing.GatherKeys)
                {
                    if (entry.VisualStateCount is null)
                        continue;

                    if (!_sprite.LayerMapTryGet(visuals.Owner, $"{PlantVisualLayers.Produce.ToString()}-{key}", out _, false))
                        continue;

                    var growthState = ContentHelpers.RoundToNearestLevels(entry.Growth, 1, entry.VisualStateCount.Value);

                    _sprite.LayerSetVisible(visuals.Owner, $"{PlantVisualLayers.Produce.ToString()}-{key}", growthState != 0);

                    if (growthState == 0)
                        continue;

                    _sprite.LayerSetRsiState(visuals.Owner, $"{PlantVisualLayers.Produce.ToString()}-{key}",
                        $"produce-{key}-var{visuals.Comp.SelectedVariation}-state{growthState}");
                    // We do some complex string formula, combining final variation states + produce growth states
                    // state: produce-[KEY]-var[VARIATION STATE NUMBER]-state[GROWTH STATE NUMBER]
                    // map: ["Produce-[KEY]"]
                }
            }
        }
    }
}
