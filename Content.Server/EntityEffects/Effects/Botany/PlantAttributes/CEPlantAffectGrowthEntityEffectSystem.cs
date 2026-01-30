using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Content.Shared._CE.Farming.Components;
using Content.Server._CE.Farming;
using Robust.Shared.GameStates;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes
{
    public sealed partial class CEPlantAffectGrowthEntityEffectSystem : EntityEffectSystem<CEPlantComponent, PlantAffectGrowth>
    {
        [Dependency] private readonly CEFarmingSystem _ceFarming = default!;

        protected override void Effect(Entity<CEPlantComponent> entity, ref EntityEffectEvent<PlantAffectGrowth> args)
        {
            if (entity.Comp == null)
                return;

            // PlantAffectGrowth.Amount is a float. CEFarmingSystem.AffectGrowth takes float.
            _ceFarming.AffectGrowth(entity, args.Effect.Amount);
        }
    }
}
