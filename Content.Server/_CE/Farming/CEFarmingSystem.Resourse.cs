using Content.Shared._CE.Farming.Components;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Server._CE.Farming;

public sealed partial class CEFarmingSystem
{
    private void InitializeResources()
    {
        SubscribeLocalEvent<CEPlantEnergyFromLightComponent, CEPlantUpdateEvent>(OnTakeEnergyFromLight);
        SubscribeLocalEvent<CEPlantMetabolizerComponent, CEPlantUpdateEvent>(OnPlantMetabolizing);
        SubscribeLocalEvent<CEPlantProducingComponent, CEPlantUpdateEvent>(OnPlantProducing);

        SubscribeLocalEvent<CEPlantGrowingComponent, CEAfterPlantUpdateEvent>(OnPlantGrowing);
    }

    private void OnTakeEnergyFromLight(Entity<CEPlantEnergyFromLightComponent> regeneration, ref CEPlantUpdateEvent args)
    {
        var gainEnergy = false;
        var daylight = _dayCycle.UnderSunlight(regeneration);

        if (regeneration.Comp.Daytime && daylight)
            gainEnergy = true;

        if (regeneration.Comp.Nighttime && !daylight)
            gainEnergy = true;

        if (gainEnergy)
            AffectEnergy(args.Plant, regeneration.Comp.Energy);
    }

    private void OnPlantMetabolizing(Entity<CEPlantMetabolizerComponent> ent, ref CEPlantUpdateEvent args)
    {
        if (!SolutionQuery.TryComp(args.Plant, out var solmanager))
            return;

        var solEntity = new Entity<SolutionContainerManagerComponent?>(args.Plant, solmanager);
        if (!_solutionContainer.TryGetSolution(solEntity, ent.Comp.Solution, out var soln, out _))
            return;

        var splitted = _solutionContainer.SplitSolution(soln.Value, ent.Comp.SolutionPerUpdate);
        foreach (var reagent in splitted)
        {
            if (!ent.Comp.Metabolization.TryGetValue(reagent.Reagent.ToString(), out var effects))
                continue;

            var reagentPercentage = reagent.Quantity / ent.Comp.SolutionPerUpdate;
            foreach (var effect in effects)
            {
                effect.Effect((ent, args.Plant.Comp), reagentPercentage, EntityManager);
            }
        }
    }

    private void OnPlantProducing(Entity<CEPlantProducingComponent> ent, ref CEPlantUpdateEvent args)
    {
        var plant = args.Plant.Comp;

        if (plant.GrowthLevel < 1) //We dont grow fruits before fully grown plant
            return;

        foreach (var (key, entry) in ent.Comp.GatherKeys)
        {
            var energyCost = entry.EnergyCost * entry.GrowthPerUpdate;
            var resourceCost = entry.ResourceCost * entry.GrowthPerUpdate;
            if (plant.Energy < energyCost)
                continue;

            if (plant.Resource < resourceCost)
                continue;

            if (entry.Growth >= 1)
                continue;

            AffectEnergy(args.Plant, -energyCost);
            AffectResource(args.Plant, -resourceCost);

            entry.Growth = MathF.Min(entry.Growth + entry.GrowthPerUpdate, 1);
            Dirty(ent);
        }
    }

    private void OnPlantGrowing(Entity<CEPlantGrowingComponent> growing, ref CEAfterPlantUpdateEvent args)
    {
        if (args.Plant.Comp.Energy < growing.Comp.EnergyCost)
            return;

        if (args.Plant.Comp.Resource < growing.Comp.ResourceCost)
            return;

        if (args.Plant.Comp.GrowthLevel >= 1)
            return;

        AffectEnergy(args.Plant, -growing.Comp.EnergyCost);
        AffectResource(args.Plant, -growing.Comp.ResourceCost);
        AffectGrowth(args.Plant, growing.Comp.GrowthPerUpdate);
    }
}
