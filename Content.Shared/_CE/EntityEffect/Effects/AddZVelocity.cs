using Content.Shared._CE.ZLevels.Core.EntitySystems;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class AddZVelocity : CEEntityEffectBase<AddZVelocity>
{
    [DataField(required: true)]
    public float Speed = 0f;

    [DataField]
    public bool RequiresGround = false;
}

public sealed partial class CEAddZVelocitySystem : CEEntityEffectSystem<AddZVelocity>
{
    [Dependency] private CESharedZLevelsSystem _zLevel = default!;

    protected override void Effect(ref CEEntityEffectEvent<AddZVelocity> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (args.Effect.RequiresGround)
        {
            if (_zLevel.DistanceToGround(entity) > 0.1f)
                return;
        }

        _zLevel.AddZVelocity(entity, args.Effect.Speed * args.Args.Power);
    }
}
