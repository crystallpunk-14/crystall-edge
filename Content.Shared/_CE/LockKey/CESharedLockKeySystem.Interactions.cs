using System.Linq;
using Content.Shared._CE.LockKey.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Storage;

namespace Content.Shared._CE.LockKey;

public abstract partial class CESharedLockKeySystem
{
    private void VerbsInteractions()
    {
        SubscribeLocalEvent<CEKeyComponent, AfterInteractEvent>(OnKeyInteract);
        SubscribeLocalEvent<CEKeyRingComponent, AfterInteractEvent>(OnKeyRingInteract);
        SubscribeLocalEvent<CELockComponent, AfterInteractEvent>(OnLockInteract);
    }

    private void OnKeyInteract(Entity<CEKeyComponent> key, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true })
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (!_lockQuery.TryComp(args.Target, out _))
            return;

        if (!_ceLockQuery.TryComp(args.Target, out var ceLockComponent))
            return;

        args.Handled = true;
        UseKeyOnLock(args.User, new Entity<CELockComponent>(args.Target.Value, ceLockComponent), key);
    }

    private void OnKeyRingInteract(Entity<CEKeyRingComponent> keyring, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true })
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<StorageComponent>(keyring, out var storageComp))
            return;

        if (!_lockQuery.TryComp(args.Target, out _))
            return;

        if (!_ceLockQuery.TryComp(args.Target, out var ceLockComponent))
            return;

        args.Handled = true;

        foreach (var (key, _) in storageComp.StoredItems)
        {
            if (!_keyQuery.TryComp(key, out var keyComp))
                continue;

            if (!keyComp.Shape.SequenceEqual(ceLockComponent.Shape))
                continue;

            UseKeyOnLock(args.User,
                new Entity<CELockComponent>(args.Target.Value, ceLockComponent),
                new Entity<CEKeyComponent>(key, keyComp));
            args.Handled = true;
            return;
        }

        if (_timing.IsFirstTimePredicted)
            _popup.PopupPredicted(Loc.GetString("ce-lock-key-no-fit"), args.Target.Value, args.User);
    }

    private void OnLockInteract(Entity<CELockComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (!ent.Comp.CanEmbedded)
            return;

        if (!_lockQuery.TryComp(args.Target, out _))
            return;

        args.Handled = true;

        //Ok, all checks passed, we ready to install lock into entity

        _popup.PopupPredicted(Loc.GetString("ce-lock-insert-start", ("name", MetaData(args.Target.Value).EntityName), ("player", Identity.Name(args.User, EntityManager))),
            ent,
            args.User);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.EmbedDelay,
            new LockInsertDoAfterEvent(),
            args.Target,
            args.Target,
            ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
        });
    }
}
