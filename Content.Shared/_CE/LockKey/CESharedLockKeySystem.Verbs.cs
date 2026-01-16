using Content.Shared._CE.LockKey.Components;
using Content.Shared.DoAfter;
using Content.Shared.Timing;
using Content.Shared.Verbs;

namespace Content.Shared._CE.LockKey;

public abstract partial class CESharedLockKeySystem
{
    private void VerbsInit()
    {
        SubscribeLocalEvent<CEKeyComponent, GetVerbsEvent<UtilityVerb>>(GetKeysVerbs);
        SubscribeLocalEvent<CEKeyFileComponent, GetVerbsEvent<UtilityVerb>>(GetKeyFileVerbs);
        SubscribeLocalEvent<CELockpickComponent, GetVerbsEvent<UtilityVerb>>(GetLockpickVerbs);
        SubscribeLocalEvent<CELockEditorComponent, GetVerbsEvent<UtilityVerb>>(GetLockEditorVerbs);
    }

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

    private void GetKeyFileVerbs(Entity<CEKeyFileComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!_keyQuery.TryComp(args.Target, out var keyComp))
            return;

        var target = args.Target;
        var user = args.User;

        var lockShapeCount = keyComp.Shape.Count;
        for (var i = 0; i <= lockShapeCount - 1; i++)
        {
            var i1 = i;
            var verb = new UtilityVerb
            {
                Act = () =>
                {
                    if (keyComp.Shape[i1] <= -DepthComplexity)
                        return;

                    if (!_timing.IsFirstTimePredicted)
                        return;

                    if (TryComp<UseDelayComponent>(ent, out var useDelayComp) &&
                        _useDelay.IsDelayed((ent, useDelayComp)))
                        return;

                    if (!_net.IsServer)
                        return;

                    keyComp.Shape[i1]--;
                    DirtyField(target, keyComp, nameof(CEKeyComponent.Shape));
                    _audio.PlayPvs(ent.Comp.UseSound, Transform(target).Coordinates);
                    Spawn("EffectSparks", Transform(target).Coordinates);
                    var shapeString = "[" + string.Join(", ", keyComp.Shape) + "]";
                    _popup.PopupEntity(Loc.GetString("ce-lock-key-file-updated") + shapeString, target, user);
                    _useDelay.TryResetDelay(ent);
                },
                IconEntity = GetNetEntity(ent),
                Category = VerbCategory.CELock,
                Priority = -i,
                Disabled = keyComp.Shape[i] <= -DepthComplexity,
                Text = Loc.GetString("ce-lock-key-file-use-hint", ("num", i)),
                CloseMenu = false,
            };
            args.Verbs.Add(verb);
        }
    }

    private void GetLockpickVerbs(Entity<CELockpickComponent> lockPick, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!_lockQuery.TryComp(args.Target, out var lockComp) || !lockComp.Locked)
            return;

        if (!_ceLockQuery.HasComp(args.Target))
            return;

        var target = args.Target;
        var user = args.User;

        for (var i = DepthComplexity; i >= -DepthComplexity; i--)
        {
            var height = i;
            var verb = new UtilityVerb()
            {
                Act = () =>
                {
                    _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                        user,
                        lockPick.Comp.HackTime,
                        new LockPickHackDoAfterEvent(height),
                        target,
                        target,
                        lockPick)
                    {
                        BreakOnDamage = true,
                        BreakOnMove = true,
                        BreakOnDropItem = true,
                        BreakOnHandChange = true,
                    });
                },
                Text = Loc.GetString("ce-lock-verb-lock-pick-use-text") + $" {height}",
                Message = Loc.GetString("ce-lock-verb-lock-pick-use-message"),
                Category = VerbCategory.CELock,
                Priority = height,
                CloseMenu = false,
            };

            args.Verbs.Add(verb);
        }
    }

    private void GetLockEditorVerbs(Entity<CELockEditorComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!_ceLockQuery.TryComp(args.Target, out var lockComp) || !lockComp.CanEmbedded)
            return;

        var target = args.Target;
        var user = args.User;

        var lockShapeCount = lockComp.Shape.Count;
        for (var i = 0; i <= lockShapeCount - 1; i++)
        {
            var i1 = i;
            var verb = new UtilityVerb
            {
                Act = () =>
                {
                    if (!_timing.IsFirstTimePredicted)
                        return;

                    if (TryComp<UseDelayComponent>(ent, out var useDelayComp) &&
                        _useDelay.IsDelayed((ent, useDelayComp)))
                        return;

                    if (!_net.IsServer)
                        return;

                    lockComp.Shape[i1]--;
                    if (lockComp.Shape[i1] < -DepthComplexity)
                        lockComp.Shape[i1] = DepthComplexity; //Cycle back to max

                    DirtyField(target, lockComp, nameof(CELockComponent.Shape));
                    _audio.PlayPvs(ent.Comp.UseSound, Transform(target).Coordinates);
                    var shapeString = "[" + string.Join(", ", lockComp.Shape) + "]";
                    _popup.PopupEntity(Loc.GetString("ce-lock-editor-updated") + shapeString, target, user);
                    _useDelay.TryResetDelay(ent);
                },
                IconEntity = GetNetEntity(ent),
                Category = VerbCategory.CELock,
                Priority = -i,
                Text = Loc.GetString("ce-lock-editor-use-hint", ("num", i)),
                CloseMenu = false,
            };
            args.Verbs.Add(verb);
        }
    }
}
