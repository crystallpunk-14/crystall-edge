using System.Numerics;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellThrowFromUser : CESpellEffect
{
    [DataField]
    public float ThrowPower = 10f;

    [DataField]
    public float Distance = 2.5f;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null || args.User is null)
            return;

        var targetEntity = args.Target.Value;

        var throwing = entManager.System<ThrowingSystem>();
        var xform = entManager.System<SharedTransformSystem>();

        var worldPos = xform.GetWorldPosition(args.User.Value);
        var dir = xform.GetWorldPosition(args.Target.Value) - worldPos;
        if (dir == Vector2.Zero)
            return;

        var foo = Vector2.Normalize(dir);

        if (entManager.TryGetComponent<EmbeddableProjectileComponent>(targetEntity, out var embeddable))
        {
            var projectile = entManager.System<SharedProjectileSystem>();

            projectile.EmbedDetach(targetEntity, embeddable);
        }

        throwing.TryThrow(targetEntity, foo * Distance, ThrowPower, args.User, doSpin: true);
    }
}
