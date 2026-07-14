using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Throwing;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class Dash : CEEntityEffectBase<Dash>
{
    public Dash()
    {
        EffectTarget = CEEffectTarget.User;
    }

    [DataField]
    public float Speed = 10f;

    [DataField]
    public float Distance = 1f;
}

public sealed partial class CEDashEffectSystem : CEEntityEffectSystem<Dash>
{
    [Dependency] private ThrowingSystem _throwing = default!;

    [Dependency] private EntityQuery<CEZPhysicsComponent> _zPhys = default!;

    protected override void Effect(ref CEEntityEffectEvent<Dash> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (_zPhys.TryComp(args.Args.Target, out var zPhys))
        {
            if (zPhys.LocalPosition > 0) //Can't dash in air
                return;
        }

        _throwing.TryThrow(
            entity,
            args.Args.Angle.ToWorldVec() * (args.Effect.Distance * args.Args.Power),
            args.Effect.Speed * args.Args.Power,
            entity,
            animated: false,
            doSpin: false);
    }
}
