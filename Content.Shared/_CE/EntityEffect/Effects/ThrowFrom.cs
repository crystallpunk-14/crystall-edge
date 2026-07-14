using System.Numerics;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Throws the effect target away from the entity resolved by <see cref="Origin"/>.
/// </summary>
public sealed partial class ThrowFrom : CEEntityEffectBase<ThrowFrom>
{
    [DataField]
    public CEEffectTarget Origin = CEEffectTarget.User;

    [DataField]
    public float ThrowPower = 10f;

    [DataField]
    public float Distance = 2.5f;
}

public sealed partial class CEThrowFromEffectSystem : CEEntityEffectSystem<ThrowFrom>
{
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedProjectileSystem _projectile = default!;

    protected override void Effect(ref CEEntityEffectEvent<ThrowFrom> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } targetEntity)
            return;

        var originEntity = ResolveEffectEntity(args.Args, args.Effect.Origin) ?? args.Args.Source;
        var fromWorldPos = _transform.GetWorldPosition(originEntity);
        var targetWorldPos = _transform.GetWorldPosition(targetEntity);
        var dir = targetWorldPos - fromWorldPos;
        if (dir == Vector2.Zero)
            return;

        if (TryComp<EmbeddableProjectileComponent>(targetEntity, out var embeddable))
            _projectile.EmbedDetach(targetEntity, embeddable);

        _throwing.TryThrow(
            targetEntity,
            Vector2.Normalize(dir) * (args.Effect.Distance * args.Args.Power),
            args.Effect.ThrowPower * args.Args.Power,
            doSpin: true);
    }
}
