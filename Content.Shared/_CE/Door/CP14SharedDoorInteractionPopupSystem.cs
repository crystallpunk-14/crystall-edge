using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Door;

public sealed class CEDoorInteractionPopupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEDoorInteractionPopupComponent, ActivateInWorldEvent>(OnActivatedInWorld);
    }

    private void OnActivatedInWorld(Entity<CEDoorInteractionPopupComponent> door, ref ActivateInWorldEvent args)
    {
        if (TryComp<LockComponent>(args.Target, out var lockComponent) && !lockComponent.Locked)
            return;

        var curTime = _timing.CurTime;

        if (curTime < door.Comp.LastInteractTime + door.Comp.InteractDelay)
            return;

        _popup.PopupPredicted(Loc.GetString(door.Comp.InteractString), args.Target, args.Target);
        _audio.PlayPredicted(door.Comp.InteractSound, args.Target, args.Target);

        door.Comp.LastInteractTime = curTime;
    }
}
