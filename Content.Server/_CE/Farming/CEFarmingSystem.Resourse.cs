using Content.Shared._CE.Farming.Components;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Server._CE.Farming;

public sealed partial class CEFarmingSystem
{
    private void InitializeResources()
    {
        SubscribeLocalEvent<CEPlantEnergyFromLightComponent, CEPlantUpdateEvent>(OnTakeEnergyFromLight);
        SubscribeLocalEvent<CEPlantMetabolizerComponent, CEPlantUpdateEvent>(OnPlantMetabolizing);

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
            args.EnergyDelta += regeneration.Comp.Energy;
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

    private void OnPlantMetabolizing(Entity<CEPlantMetabolizerComponent> ent, ref CEPlantUpdateEvent args)
    {
        if (!PlantQuery.TryComp(ent, out var plant) ||
            !SolutionQuery.TryComp(args.Plant, out var solmanager))
            return;

        var solEntity = new Entity<SolutionContainerManagerComponent?>(args.Plant, solmanager);
        if (!_solutionContainer.TryGetSolution(solEntity, plant.Solution, out var soln, out _))
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
                effect.Effect((ent, plant), reagent.Quantity, EntityManager);
            }
        }
    }
}
