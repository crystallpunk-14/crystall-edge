using Content.Shared.Projectiles;
using Content.Shared.Throwing;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Throws the effect target toward the entity resolved by <see cref="Destination"/>.
/// </summary>
public sealed partial class ThrowTo : CEEntityEffectBase<ThrowTo>
{
    [DataField]
    public CEEffectTarget Destination = CEEffectTarget.User;

    [DataField]
    public float ThrowPower = 10f;
}

public sealed partial class CEThrowToEffectSystem : CEEntityEffectSystem<ThrowTo>
{
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedProjectileSystem _projectile = default!;

    protected override void Effect(ref CEEntityEffectEvent<ThrowTo> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } targetEntity)
            return;

        if (ResolveEffectEntity(args.Args, args.Effect.Destination) is not { } destinationEntity)
            return;

        var destCoords = Transform(destinationEntity).Coordinates;

        if (TryComp<EmbeddableProjectileComponent>(targetEntity, out var embeddable))
            _projectile.EmbedDetach(targetEntity, embeddable);

        _throwing.TryThrow(targetEntity, destCoords, args.Effect.ThrowPower * args.Args.Power);
    }
}
