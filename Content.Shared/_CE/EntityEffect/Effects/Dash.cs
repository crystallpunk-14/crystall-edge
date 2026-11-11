using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;

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

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-dash", ("distance", Distance));
}

public sealed partial class CEDashEffectSystem : CEEntityEffectSystem<Dash>
{
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;

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

        var distance = args.Effect.Distance * args.Args.Power;
        var speed = args.Effect.Speed * args.Args.Power;

        // Movement friction is removed for the flight time, otherwise it eats most of a long dash.
        // Negative distance dashes backwards, so the flight time uses its magnitude
        if (speed > 0)
            _movementMod.TryAddFrictionModDuration(entity, TimeSpan.FromSeconds(MathF.Abs(distance) / speed), 0f);

        _throwing.TryThrow(
            entity,
            args.Args.Angle.ToWorldVec() * distance,
            speed,
            entity,
            animated: false,
            doSpin: false);
    }
}
