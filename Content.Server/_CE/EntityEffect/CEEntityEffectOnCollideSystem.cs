using Content.Shared._CE.EntityEffect;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server._CE.EntityEffect;

public sealed class CEEntityEffectOnCollideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEEntityEffectOnCollideComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<CEEntityEffectOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.TriggerFixtureId)
            return;

        if (!ent.Comp.AffectAirborne
            && TryComp<PhysicsComponent>(args.OtherEntity, out var otherPhysics)
            && otherPhysics.BodyStatus == BodyStatus.InAir)
            return;

        foreach (var effect in ent.Comp.Effects)
        {
            var effectArgs = new CEEntityEffectArgs(
                EntityManager,
                Source: ent,
                Used: ent,
                Angle: Angle.Zero,
                Speed: 0f,
                Target: args.OtherEntity,
                Position: null);

            effect.Effect(effectArgs);
        }
    }
}
