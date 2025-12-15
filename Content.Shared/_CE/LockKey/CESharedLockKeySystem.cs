using System.Linq;
using System.Text;
using Content.Shared._CE.LockKey.Components;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._CE.LockKey;

public abstract partial class CESharedLockKeySystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<LockComponent> _lockQuery;
    private EntityQuery<CELockComponent> _ceLockQuery;
    private EntityQuery<CEKeyComponent> _keyQuery;
    private EntityQuery<DoorComponent> _doorQuery;

    public const int DepthComplexity = 2;

    public override void Initialize()
    {
        base.Initialize();

        VerbsInit();
        VerbsInteractions();
        DirectionalLatchInit();

        _lockQuery = GetEntityQuery<LockComponent>();
        _ceLockQuery = GetEntityQuery<CELockComponent>();
        _keyQuery = GetEntityQuery<CEKeyComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();

        SubscribeLocalEvent<CELockComponent, LockPickHackDoAfterEvent>(OnLockHacked);
        SubscribeLocalEvent<CELockComponent, LockInsertDoAfterEvent>(OnLockInserted);

        SubscribeLocalEvent<CEKeyComponent, ExaminedEvent>(OnKeyExamine);
        SubscribeLocalEvent<CELockComponent, ExaminedEvent>(OnLockExamine);
    }

    private void OnLockInserted(Entity<CELockComponent> ent, ref LockInsertDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!_ceLockQuery.TryComp(args.Used, out var usedLock))
            return;

        if (!TrySetShape((ent, ent.Comp), usedLock.Shape))
            return;

        _popup.PopupPredicted(Loc.GetString("ce-lock-insert-success", ("name", MetaData(ent).EntityName)),
            ent,
            args.User);

        _audio.PlayPredicted(usedLock.EmbedSound, ent, args.User);

        if (_net.IsServer)
            QueueDel(args.Used);
    }

    public bool TrySetShape(Entity<CELockComponent?> ent, List<int>? shape)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (shape == null || shape.Count == 0)
            return false;

        ent.Comp.Shape = new List<int>(shape);
        DirtyField(ent, ent.Comp, nameof(CELockComponent.Shape));
        return true;
    }

    public bool TrySetShape(Entity<CEKeyComponent?> ent, List<int>? shape)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (shape == null || shape.Count == 0)
            return false;

        ent.Comp.Shape = new List<int>(shape);
        DirtyField(ent, ent.Comp, nameof(CEKeyComponent.Shape));
        return true;
    }

    private void OnLockHacked(Entity<CELockComponent> ent, ref LockPickHackDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!_lockQuery.TryComp(ent, out var lockComp))
            return;

        if (!TryComp<CELockpickComponent>(args.Used, out var lockPick))
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Height == ent.Comp.Shape[ent.Comp.LockPickStatus]) //Success
        {
            _audio.PlayPredicted(lockPick.SuccessSound, ent, args.User);
            ent.Comp.LockPickStatus++;
            DirtyField(ent, ent.Comp, nameof(CELockComponent.LockPickStatus));

            if (ent.Comp.LockPickStatus >= ent.Comp.Shape.Count) // Final success
            {
                if (lockComp.Locked)
                {
                    _lock.TryUnlock(ent, args.User, lockComp);
                    _popup.PopupPredicted(Loc.GetString("ce-lock-unlock", ("lock", MetaData(ent).EntityName)),
                        ent,
                        args.User);

                    ent.Comp.LockPickStatus = 0;
                    DirtyField(ent, ent.Comp, nameof(CELockComponent.LockPickStatus));
                    return;
                }

                _lock.TryLock(ent, args.User, lockComp);

                _popup.PopupPredicted(Loc.GetString("ce-lock-lock", ("lock", MetaData(ent).EntityName)),
                    ent,
                    args.User);
                ent.Comp.LockPickStatus = 0;

                DirtyField(ent, ent.Comp, nameof(CELockComponent.LockPickStatus));
                return;
            }

            _popup.PopupClient(Loc.GetString("ce-lock-lock-pick-success") +
                               $" ({ent.Comp.LockPickStatus}/{ent.Comp.Shape.Count})",
                ent,
                args.User);
        }
        else //Fail
        {
            _audio.PlayPredicted(lockPick.FailSound, ent, args.User);
            if (_net.IsServer)
            {
                lockPick.Health--;
                if (lockPick.Health > 0)
                {
                    _popup.PopupEntity(Loc.GetString("ce-lock-lock-pick-failed", ("lock", MetaData(ent).EntityName)),
                        ent,
                        args.User);
                }
                else
                {
                    _popup.PopupEntity(
                        Loc.GetString("ce-lock-lock-pick-failed-break", ("lock", MetaData(ent).EntityName)),
                        ent,
                        args.User);
                    QueueDel(args.Used);
                }
            }

            ent.Comp.LockPickStatus = 0;
            DirtyField(ent, ent.Comp, nameof(CELockComponent.LockPickStatus));
        }
    }

    private void UseKeyOnLock(EntityUid user, Entity<CELockComponent> target, Entity<CEKeyComponent> key)
    {
        if (!TryComp<LockComponent>(target, out var lockComp))
            return;

        if (_doorQuery.TryComp(target, out var doorComponent) && doorComponent.State == DoorState.Open)
        {
            if (!_door.TryClose(target, doorComponent, user, true))
                return;
        }

        var keyShape = key.Comp.Shape;
        var lockShape = target.Comp.Shape;

        var isEqual = keyShape.SequenceEqual(lockShape);

        // Make new shape for key and force equality for this use
        if (HasComp<CEKeyUniversalComponent>(key) && !isEqual && TrySetShape((key, key.Comp), lockShape))
        {
            _popup.PopupClient(Loc.GetString("ce-lock-key-transforming"), key, user);
            isEqual = true;
        }

        if (isEqual)
        {
            if (lockComp.Locked)
                _lock.TryUnlock(target, user);
            else
                _lock.TryLock(target, user);
        }
        else
            _popup.PopupClient(Loc.GetString("ce-lock-key-no-fit"), target, user);
    }

    private void OnKeyExamine(Entity<CEKeyComponent> ent, ref ExaminedEvent args)
    {
        var parent = Transform(ent).ParentUid;
        if (parent != args.Examiner)
            return;

        var sb = new StringBuilder(Loc.GetString("ce-lock-examine-key", ("item", MetaData(ent).EntityName)));
        sb.Append(" (");
        foreach (var item in ent.Comp.Shape)
        {
            sb.Append($"{item} ");
        }

        sb.Append(")");
        args.PushMarkup(sb.ToString());
    }

    private void OnLockExamine(Entity<CELockComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.CanEmbedded)
            return;

        var parent = Transform(ent).ParentUid;
        if (parent != args.Examiner)
            return;

        if (ent.Comp.Shape == null)
            return;

        var sb = new StringBuilder(Loc.GetString("ce-lock-examine-key", ("item", MetaData(ent).EntityName)));
        sb.Append(" (");
        foreach (var item in ent.Comp.Shape)
        {
            sb.Append($"{item} ");
        }

        sb.Append(")");
        args.PushMarkup(sb.ToString());
    }
}

[Serializable, NetSerializable]
public sealed partial class LockPickHackDoAfterEvent : DoAfterEvent
{
    [DataField]
    public int Height = 0;

    public LockPickHackDoAfterEvent(int h)
    {
        Height = h;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class LockInsertDoAfterEvent : SimpleDoAfterEvent;
