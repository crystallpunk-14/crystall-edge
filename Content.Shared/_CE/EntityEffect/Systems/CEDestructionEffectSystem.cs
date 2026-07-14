using Content.Shared.Destructible;
using Robust.Shared.Spawners;

namespace Content.Shared._CE.EntityEffect.Systems;

public sealed class CEDestructionEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEDestructionEffectComponent, DestructionEventArgs>(OnDestructed);
        SubscribeLocalEvent<CEDestructionEffectComponent, TimedDespawnEvent>(OnDespawn);
    }

    private void OnDespawn(Entity<CEDestructionEffectComponent> ent, ref TimedDespawnEvent args)
    {
        var effectArgs = new CEEntityEffectArgs(
            EntityManager,
            ent.Owner,
            null,
            Angle.Zero,
            0f,
            null,
            Transform(ent).Coordinates);

        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(effectArgs);
        }
    }

    private void OnDestructed(Entity<CEDestructionEffectComponent> ent, ref DestructionEventArgs args)
    {
        var effectArgs = new CEEntityEffectArgs(
            EntityManager,
            ent.Owner,
            null,
            Angle.Zero,
            0f,
            null,
            Transform(ent).Coordinates);

        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(effectArgs);
        }
    }
}
