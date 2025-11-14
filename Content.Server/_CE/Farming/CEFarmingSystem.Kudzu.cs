using System.Linq;
using Content.Server.Spreader;
using Content.Shared._CE.Farming.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._CE.Farming;

public sealed partial class CEFarmingSystem
{
    private void InitializeKudzu()
    {
        SubscribeLocalEvent<CEPlantKudzuComponent, SpreadNeighborsEvent>(OnKudzuSpread);
        SubscribeLocalEvent<CEPlantKudzuComponent, CEAfterPlantUpdateEvent>(OnKudzuUpdate);
    }

    private void OnKudzuUpdate(Entity<CEPlantKudzuComponent> ent, ref CEAfterPlantUpdateEvent args)
    {
        if (!PlantQuery.TryComp(ent, out var plant))
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        if (plant.Resource < ent.Comp.ResourceCost || plant.Energy < ent.Comp.EnergyCost)
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
        else
            EnsureComp<ActiveEdgeSpreaderComponent>(ent);
    }

    private void OnKudzuSpread(Entity<CEPlantKudzuComponent> ent, ref SpreadNeighborsEvent args)
    {
        if (!PlantQuery.TryComp(ent, out var plant))
            return;

        if (plant.GrowthLevel < 1f)
            return;

        if (plant.Energy < ent.Comp.EnergyCost)
            return;

        if (plant.Resource < ent.Comp.ResourceCost)
            return;

        var targetPositions = new List<EntityCoordinates>();
        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var position = MapSystem.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices);
            if (!CanPlant(ent.Comp.Proto, position, null, ent))
                continue;

            targetPositions.Add(position);
        }

        if (targetPositions.Count == 0)
            return;

        var positionToSpawn = targetPositions.Count > 1
            ? _random.Pick(targetPositions)
            : targetPositions.First();

        Spawn(ent.Comp.Proto, positionToSpawn);

        AffectEnergy((ent, plant), -ent.Comp.EnergyCost);
        AffectResource((ent, plant), -ent.Comp.ResourceCost);
    }
}
