using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.Analyzers;
using Robust.Shared.Physics.Components;

namespace Content.Shared._CE.AnimationController;

/// <summary>
/// Internal component tracking movement state for idle/walk animation selection.
/// Not networked — per-side state only.
/// </summary>
[RegisterComponent]
internal sealed partial class CEMovementStateComponent : Component
{
    public bool IsMoving;
}

public sealed partial class CEIdleWalkAnimationSystem : EntitySystem
{
    [Dependency] private CEAnimationControllerSystem _controller = default!;

    private const float MovingThresholdSq = 0.04f; // 0.2 m/s

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEAnimationControllerComponent> ent, ref MapInitEvent args)
    {
        _controller.RefreshVisuals((ent, ent.Comp));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEAnimationControllerComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var controller, out var physics))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            // Skip player-controlled entities — they update via SpriteMoveEvent.
            if (HasComp<InputMoverComponent>(uid))
                continue;

            var moving = physics.LinearVelocity.LengthSquared() >= MovingThresholdSq;
            var state = EnsureComp<CEMovementStateComponent>(uid);
            if (moving == state.IsMoving)
                continue;

            state.IsMoving = moving;
            _controller.RefreshVisuals((uid, controller));
        }
    }

    [SubscribeLocalEvent]
    private void OnSpriteMoveEvent(Entity<CEAnimationControllerComponent> ent, ref SpriteMoveEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var state = EnsureComp<CEMovementStateComponent>(ent);
        if (state.IsMoving == args.IsMoving)
            return;

        state.IsMoving = args.IsMoving;
        _controller.RefreshVisuals((ent, ent.Comp));
    }

    [SubscribeLocalEvent]
    private void OnIdleAppearance(Entity<CEIdleAnimationComponent> ent, ref CECalculateCurrentAppearanceEvent args)
    {
        if (ent.Comp.AppearanceKey is { } key)
            args.Set(key, 0);
    }

    [SubscribeLocalEvent]
    private void OnIdleAnimation(Entity<CEIdleAnimationComponent> ent, ref CECalculateCurrentAnimationEvent args)
    {
        if (ent.Comp.Animation is { } anim)
            args.Set(anim, 0);
    }

    [SubscribeLocalEvent]
    private void OnWalkAppearance(Entity<CEWalkingAnimationComponent> ent, ref CECalculateCurrentAppearanceEvent args)
    {
        if (!TryComp<CEMovementStateComponent>(ent, out var movementComp) || !movementComp.IsMoving)
            return;

        if (ent.Comp.AppearanceKey is { } key)
            args.Set(key, 1);
    }

    [SubscribeLocalEvent]
    private void OnWalkAnimation(Entity<CEWalkingAnimationComponent> ent, ref CECalculateCurrentAnimationEvent args)
    {
        if (!TryComp<CEMovementStateComponent>(ent, out var movementComp) || !movementComp.IsMoving)
            return;

        if (ent.Comp.Animation is { } anim)
            args.Set(anim, 1);
    }
}

