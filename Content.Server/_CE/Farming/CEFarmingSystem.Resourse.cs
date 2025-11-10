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
        if (!_solutionContainer.TryGetSolution(solEntity, args.Plant.Comp.Solution, out var soln, out _))
            return;

        if (!_proto.Resolve(ent.Comp.MetabolizerId, out var metabolizer))
            return;

        var splitted = _solutionContainer.SplitSolution(soln.Value, ent.Comp.SolutionPerUpdate);
        foreach (var reagent in splitted)
        {
            if (!metabolizer.Metabolization.TryGetValue(reagent.Reagent.ToString(), out var effects))
                continue;

            foreach (var effect in effects)
            {
                effect.Effect((ent, args.Plant.Comp), reagent.Quantity, EntityManager);
            }
        }
    }

    private void OnPlantProducing(Entity<CEPlantProducingComponent> ent, ref CEPlantUpdateEvent args)
    {
        var plant = args.Plant.Comp;

        if (plant.GrowthLevel < 1) //We dont grow fruits before fully grown plant
            return;

        foreach (var (_, gatherEntry) in ent.Comp.Produce)
        {
            var energyCost = gatherEntry.EnergyCost * gatherEntry.GrowthPerUpdate;
            var resourceCost = gatherEntry.ResourceCost * gatherEntry.GrowthPerUpdate;
            if (plant.Energy < energyCost)
                continue;

            if (plant.Resource < resourceCost)
                continue;

            if (gatherEntry.Growth >= 1)
                continue;

            AffectEnergy(args.Plant, -energyCost);
            AffectResource(args.Plant, -resourceCost);

            gatherEntry.Growth = MathF.Min(gatherEntry.Growth + gatherEntry.GrowthPerUpdate, 1);
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
