using Content.Shared._CE.Farming.Components;
using Content.Shared.Examine;
using Content.Shared.Rounding;

namespace Content.Shared._CE.Farming;

public abstract partial class CESharedFarmingSystem
{
    private void InitializeExamine()
    {
        SubscribeLocalEvent<CEPlantAdditionalProduceOnInteractComponent, ExaminedEvent>(OnInteractGatherExamine);
        SubscribeLocalEvent<CEPlantProducingComponent, ExaminedEvent>(OnProducingExamine);
    }

    private void OnInteractGatherExamine(Entity<CEPlantAdditionalProduceOnInteractComponent> ent,
        ref ExaminedEvent args)
    {
        if (!PlantQuery.TryComp(ent, out var plant))
            return;

        if (ent.Comp.Produce.Count == 0)
            return;

        args.PushMarkup(Loc.GetString("ce-farming-harvest-examine-title"));
        foreach (var (proto, maxCount) in ent.Comp.Produce)
        {
            if (!_proto.Resolve(proto, out var indexedProto))
                continue;

            var produceName = indexedProto.Name;
            var produceCount = ContentHelpers.RoundToEqualLevels(plant.GrowthLevel, 1, maxCount + 1);

            args.PushMarkup($"[color=yellow]{produceName}:[/color] {produceCount}/{maxCount}");
        }
    }

    private void OnProducingExamine(Entity<CEPlantProducingComponent> ent, ref ExaminedEvent args)
    {
        if (!PlantQuery.TryComp(ent, out var plant))
            return;

        if (ent.Comp.GatherKeys.Count == 0)
            return;

        if (plant.GrowthLevel < 1)
        {
            args.PushMarkup(Loc.GetString("ce-farming-producing-examine-early"));
            return;
        }

        args.PushMarkup(Loc.GetString("ce-farming-harvest-examine-title"));
        foreach (var (_, entry) in ent.Comp.GatherKeys)
        {
            if (!_proto.Resolve(entry.Result, out var indexedProto))
                continue;

            var produceName = indexedProto.Name;
            var produceCount = ContentHelpers.RoundToEqualLevels(entry.Growth, 1, entry.MaxProduce + 1);

            args.PushMarkup($"[color=yellow]{produceName}:[/color] {produceCount}/{entry.MaxProduce}");
        }
    }
}
