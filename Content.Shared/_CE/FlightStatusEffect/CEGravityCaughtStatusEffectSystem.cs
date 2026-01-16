using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._CE.FlightStatusEffect;

public sealed class CEGravityCaughtStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly CESharedZLevelsSystem _zLevels = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGravityCaughtStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<CEGravityCaughtStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);

        SubscribeLocalEvent<CEGravityCaughtStatusEffectComponent, StatusEffectRelayedEvent<CECheckGravityEvent>>(OnCheckGravityState);
        SubscribeLocalEvent<CEGravityCaughtStatusEffectComponent, StatusEffectRelayedEvent<CEGetZVelocityEvent>>(OnGetZVelocity);
    }

    private void OnApplied(Entity<CEGravityCaughtStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _zLevels.UpdateGravityState(args.Target);
        _zLevels.SetZVelocity(args.Target, 0f); //Reset speed on apply
    }

    private void OnRemoved(Entity<CEGravityCaughtStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _zLevels.UpdateGravityState(args.Target);
    }

    private void OnCheckGravityState(Entity<CEGravityCaughtStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CECheckGravityEvent> args)
    {
        args.Args.Gravity *= 0;
    }

    private void OnGetZVelocity(Entity<CEGravityCaughtStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CEGetZVelocityEvent> args)
    {
        var currentPosition = args.Args.Target.Comp.LocalPosition;
        var targetPosition = 0.5f;

        var velocity = (targetPosition - currentPosition) * 0.1f;

        _zLevels.SetZVelocity((args.Args.Target.Owner, args.Args.Target.Comp), velocity);
    }
}
