using Content.Shared._CE.Farming.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._CE.Farming.Metabolizer;

public sealed partial class AffectPlantValues : CEMetabolizerEffect
{
    [DataField]
    public float Energy = 0f;
    [DataField]
    public float Resource = 0f;
    [DataField]
    public float Growth = 0f;

    public override void Effect(Entity<CEPlantComponent> plant, FixedPoint2 amount, EntityManager entityManager)
    {
        var farmingSystem = entityManager.System<CESharedFarmingSystem>();

        farmingSystem.AffectEnergy(plant, Energy * (float)amount);
        farmingSystem.AffectResource(plant,Resource * (float)amount);
        farmingSystem.AffectGrowth(plant, Growth * (float)amount);
    }
}
