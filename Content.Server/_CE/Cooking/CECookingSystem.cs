/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using Content.Server.Nutrition.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._CE.Cooking;
using Content.Shared._CE.Cooking.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Temperature;
using Robust.Shared.Random;

namespace Content.Server._CE.Cooking;

public sealed class CECookingSystem : CESharedCookingSystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEFoodHolderComponent, SolutionContainerChangedEvent>(OnHolderChanged);
        SubscribeLocalEvent<CETemperatureTransformationComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);
    }

    private void OnHolderChanged(Entity<CEFoodHolderComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.Solution.Volume != 0)
            return;

        ent.Comp.FoodData = null;
        Dirty(ent);
    }

    private void OnTemperatureChanged(Entity<CETemperatureTransformationComponent> start,
        ref OnTemperatureChangeEvent args)
    {
        var xform = Transform(start);
        foreach (var entry in start.Comp.Entries)
        {
            if (args.CurrentTemperature >= entry.TemperatureRange.X &&
                args.CurrentTemperature < entry.TemperatureRange.Y)
            {
                if (entry.TransformTo == null)
                    continue;

                SpawnNextToOrDrop(entry.TransformTo, start);
                Del(start);

                break;
            }
        }
    }

    protected override bool TryTransferFood(Entity<CEFoodHolderComponent> target, Entity<CEFoodHolderComponent> source)
    {
        if (base.TryTransferFood(target, source))
        {
            //Sliceable
            if (source.Comp.FoodData?.SliceProto is not null)
            {
                var sliceable = EnsureComp<SliceableFoodComponent>(target);
                sliceable.Slice = source.Comp.FoodData.SliceProto;
                sliceable.TotalCount = source.Comp.FoodData.SliceCount;
            }
        }

        return true;
    }

    protected override void OnCookBurned(Entity<CEFoodCookerComponent> ent, ref CEBurningDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;

        base.OnCookBurned(ent, ref args);

        //if (_random.Prob(ent.Comp.BurntAdditionalSpawnProb))
        //    Spawn(ent.Comp.BurntAdditionalSpawn, Transform(ent).Coordinates);
    }

    protected override void UpdateFoodDataVisuals(Entity<CEFoodHolderComponent> ent, CEFoodData data, bool rename = true)
    {
        base.UpdateFoodDataVisuals(ent, data, rename);

        if (ent.Comp.FoodData?.SliceProto is null)
            return;

        if (!TryComp<SliceableFoodComponent>(ent, out var sliceable))
            return;

        sliceable.Slice = ent.Comp.FoodData.SliceProto;
        sliceable.TotalCount = ent.Comp.FoodData.SliceCount;
    }

    protected override void OnCookFinished(Entity<CEFoodCookerComponent> ent, ref CECookingDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;

        //We need transform all BEFORE Shared cooking code
        TryTransformAll(ent);

        base.OnCookFinished(ent, ref args);
    }

    private void TryTransformAll(Entity<CEFoodCookerComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        var containedEntities = container.ContainedEntities.ToList();

        foreach (var contained in containedEntities)
        {
            if (!TryComp<CETemperatureTransformationComponent>(contained, out var transformable))
                continue;

            if (!transformable.AutoTransformOnCooked)
                continue;

            if (transformable.Entries.Count == 0)
                continue;

            var entry = transformable.Entries[0];

            var newTemp = (entry.TemperatureRange.X + entry.TemperatureRange.Y) / 2;
            _temperature.ForceChangeTemperature(contained, newTemp);
        }
    }
}
