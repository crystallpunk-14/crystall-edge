namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Teleports the entity chosen by <see cref="CEEntityEffect.EffectTarget"/> to the location resolved
/// from <see cref="TeleportTarget"/>. With a world-target action, <see cref="CEEffectTarget.Target"/>
/// resolves to the clicked entity, falling back to the clicked coordinates on empty ground.
/// Ignores obstacles and collision.
/// </summary>
public sealed partial class Teleport : CEEntityEffectBase<Teleport>
{
    /// <summary>
    /// Which point to teleport the entity to.
    /// </summary>
    [DataField]
    public CEEffectTarget TeleportTarget = CEEffectTarget.Target;
}

public sealed partial class CETeleportEffectSystem : CEEntityEffectSystem<Teleport>
{
    protected override void Effect(ref CEEntityEffectEvent<Teleport> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!TryResolveEffectCoordinates(args.Args, args.Effect.TeleportTarget, out var coords))
            return;

        TransformSystem.SetCoordinates(entity, coords);
    }
}
