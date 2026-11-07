using Content.Shared._CE.LockKey.Components;
using Content.Shared.Verbs;

namespace Content.Shared._CE.LockKey;

public abstract partial class CESharedLockKeySystem
{
    private void VerbsInit()
    {
    }

    [SubscribeLocalEvent]
    private void GetKeysVerbs(Entity<CEKeyComponent> key, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!_lockQuery.TryComp(args.Target, out var lockComp))
            return;

        if (!_ceLockQuery.TryComp(args.Target, out var ceLockComponent))
            return;

        var target = args.Target;
        var user = args.User;

        var verb = new UtilityVerb
        {
            Act = () =>
            {
                UseKeyOnLock(user, new Entity<CELockComponent>(target, ceLockComponent), key);
            },
            IconEntity = GetNetEntity(key),
            Text = Loc.GetString(
                lockComp.Locked ? "ce-lock-verb-use-key-text-open" : "ce-lock-verb-use-key-text-close",
                ("item", MetaData(args.Target).EntityName)),
            Message = Loc.GetString("ce-lock-verb-use-key-message", ("item", MetaData(args.Target).EntityName)),
        };

        args.Verbs.Add(verb);
    }
}
