using Content.Shared._CE.Animation.Core.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;

namespace Content.Shared._CE.Animation.Core;

public abstract partial class CESharedAnimationActionSystem
{
    private void InitMovement()
    {
        SubscribeLocalEvent<CEActiveAnimationActionComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<CEActiveAnimationActionComponent, ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);
    }

    private void OnChangeDirectionAttempt(Entity<CEActiveAnimationActionComponent> ent, ref ChangeDirectionAttemptEvent args)
    {
        if (!_proto.Resolve(ent.Comp.ActiveAnimation, out var animation))
            return;

        if (animation.LockRotation)
            args.Cancel();
    }

    private void OnRefreshMovementSpeedModifiers(Entity<CEActiveAnimationActionComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_proto.Resolve(ent.Comp.ActiveAnimation, out var animation))
            return;

        args.ModifySpeed(animation.MovementSpeed);
    }
}
