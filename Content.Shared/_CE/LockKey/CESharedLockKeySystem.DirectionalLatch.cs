using Content.Shared._CE.Door;
using Content.Shared._CE.LockKey.Components;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Shared._CE.LockKey;

public abstract partial class CESharedLockKeySystem
{
    private EntityQuery<CEDirectionalLatchComponent> _directionalLatchQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    private void DirectionalLatchInit()
    {
        _directionalLatchQuery = GetEntityQuery<CEDirectionalLatchComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<CEDirectionalLatchComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleVerb);
    }

    private void AddToggleVerb(Entity<CEDirectionalLatchComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!_lockQuery.TryComp(ent, out var lockComp))
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Disabled = !_lock.CanToggleLock((ent, lockComp), args.User),
            Act = () =>
            {

                if (!CanInteractWithDirectionalLatch(ent, user) && _timing.IsFirstTimePredicted)
                {
                    _popup.PopupClient(
                        Loc.GetString(lockComp.Locked ? "ce-directional-latch-cannot-unlock" : "ce-directional-latch-cannot-lock"),
                        ent,
                        user);

                    if (TryComp<CEDoorInteractionPopupComponent>(ent, out var doorInteraction))
                        _audio.PlayPredicted(doorInteraction.InteractSound, ent, user);

                    return;
                }

                if (lockComp.Locked)
                    _lock.TryUnlock(ent, user, lockComp);
                else
                    _lock.TryLock(ent, user, lockComp);
            },
            Text = Loc.GetString(lockComp.Locked ? "toggle-lock-verb-unlock" : "toggle-lock-verb-lock"),
            Icon = !lockComp.Locked
                ? new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/lock.svg.192dpi.png"))
                : new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/unlock.svg.192dpi.png")),
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Checks if the user can interact with a lock that has a directional latch.
    /// Returns true if the lock doesn't have a directional latch, or if the user is on the correct side.
    /// </summary>
    public bool CanInteractWithDirectionalLatch(Entity<CEDirectionalLatchComponent> target, EntityUid user)
    {
        if (!_transformQuery.TryComp(user, out var userXform) ||
            !_transformQuery.TryComp(target, out var targetXform))
            return false;

        // Get positions
        var userPos = _transform.GetWorldPosition(userXform);
        var targetPos = _transform.GetWorldPosition(targetXform);

        var directionToUser = userPos - targetPos;
        var userDirection = directionToUser.GetDir();

        var targetRotation = _transform.GetWorldRotation(target);
        var requiredWorldDirection = (targetRotation + target.Comp.Direction.ToAngle()).GetCardinalDir();

        return userDirection == requiredWorldDirection;
    }
}
