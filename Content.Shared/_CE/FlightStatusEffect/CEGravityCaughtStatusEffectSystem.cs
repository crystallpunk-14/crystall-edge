using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Gravity;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._CE.FlightStatusEffect;

public sealed class CEGravityCaughtStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGravityCaughtStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<CEGravityCaughtStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);

        SubscribeLocalEvent<CEGravityCaughtStatusEffectComponent, StatusEffectRelayedEvent<IsWeightlessEvent>>(CheckWeightless);
        SubscribeLocalEvent<CEGravityCaughtStatusEffectComponent, StatusEffectRelayedEvent<CECheckGravityEvent>>(OnCheckGravityState);
        SubscribeLocalEvent<CEGravityCaughtStatusEffectComponent, StatusEffectRelayedEvent<CEGetZVelocityEvent>>(OnGetZVelocity);
    }

    private void CheckWeightless(Entity<CEGravityCaughtStatusEffectComponent> ent, ref StatusEffectRelayedEvent<IsWeightlessEvent> args)
    {
        if (args.Args.Handled)
            return;

        var a = args.Args;
        a.IsWeightless = true;
        a.Handled = true;
    }

    private void OnApplied(Entity<CEGravityCaughtStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!TryComp<CEZPhysicsComponent>(args.Target, out var zPhyComp))
            return;

        _zLevels.UpdateGravityState(args.Target);
        _gravity.RefreshWeightless(args.Target);
        _zLevels.SetZPosition(args.Target, 0.5f);
        _zLevels.SetZVelocity(args.Target, 0); //Reset velocity on apply
    }

    private void OnRemoved(Entity<CEGravityCaughtStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _zLevels.UpdateGravityState(args.Target);
        _gravity.RefreshWeightless(args.Target);
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
