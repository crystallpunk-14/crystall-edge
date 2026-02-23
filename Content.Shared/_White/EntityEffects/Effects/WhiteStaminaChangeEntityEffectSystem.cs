using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;

namespace Content.Shared._White.EntityEffects.Effects;

public sealed partial class WhiteStaminaChangeEntityEffectSystem : EntityEffectSystem<StaminaComponent, WhiteStaminaChange>
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    protected override void Effect(Entity<StaminaComponent> entity, ref EntityEffectEvent<WhiteStaminaChange> args)
    {
        if (args.Effect.StaminaDelta < 0)
        {
            _stamina.TakeStaminaDamage(entity, -args.Effect.StaminaDelta * args.Scale);
            return;
        }

        entity.Comp.StaminaDamage = Math.Max(0, entity.Comp.StaminaDamage - args.Effect.StaminaDelta * args.Scale);
        Dirty(entity);

        _stamina.SetStaminaAlert(entity);
    }
}

public sealed partial class WhiteStaminaChange : EntityEffectBase<WhiteStaminaChange>
{
    [DataField(required: true)]
    public float StaminaDelta = 1;
}
