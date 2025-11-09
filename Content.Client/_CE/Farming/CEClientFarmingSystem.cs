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

        if (plant.GrowthLevel <= 1) //Growing
        {
            var growthState = ContentHelpers.RoundToNearestLevels(plant.GrowthLevel, 1, visuals.Comp.GrowthSteps);
            if (growthState == 0)
                growthState++;

            if (_sprite.LayerMapTryGet(visuals.Owner, PlantVisualLayers.Base, out _, false))
                _sprite.LayerSetRsiState(visuals.Owner, PlantVisualLayers.Base, $"{visuals.Comp.GrowState}{growthState}");

            if (_sprite.LayerMapTryGet(visuals.Owner, PlantVisualLayers.BaseUnshaded, out _, false))
                _sprite.LayerSetRsiState(visuals.Owner, PlantVisualLayers.BaseUnshaded, $"{visuals.Comp.GrowUnshadedState}{growthState}");
        }
        else //Fully frown
        {
            var grownVariant = _random.Next(0, visuals.Comp.ReadyVariation);

            if (_sprite.LayerMapTryGet(visuals.Owner, PlantVisualLayers.Base, out _, false))
                _sprite.LayerSetRsiState(visuals.Owner, PlantVisualLayers.Base, $"{visuals.Comp.ReadyState}{grownVariant}");

            if (_sprite.LayerMapTryGet(visuals.Owner, PlantVisualLayers.BaseUnshaded, out _, false))
                _sprite.LayerSetRsiState(visuals.Owner, PlantVisualLayers.BaseUnshaded, $"{visuals.Comp.ReadyUnshadedState}{grownVariant}");
        }

    }
}
