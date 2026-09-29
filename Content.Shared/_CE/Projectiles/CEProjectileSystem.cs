using Content.Shared._CE.EntityEffect;
using Content.Shared.Projectiles;

namespace Content.Shared._CE.Projectiles;

public sealed partial class CEProjectileSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnHit(Entity<CEProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        var effectArgs = new CEEntityEffectArgs(
            EntityManager,
            args.Shooter ?? ent,
            ent,
            Angle.Zero,
            1f,
            args.Target,
            null);

        foreach (var effect in ent.Comp.HitEffects)
        {
            effect.Effect(effectArgs);
        }
    }
}
