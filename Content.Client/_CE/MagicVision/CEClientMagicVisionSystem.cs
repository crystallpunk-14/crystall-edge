using Content.Shared._CE.MagicVision.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._CE.MagicVision;

public sealed partial class CEClientMagicVisionSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    private CEMagicVisionOverlay? _overlay;

    private readonly SoundSpecifier _startSound = new SoundPathSpecifier(new ResPath("/Audio/Effects/eye_open.ogg"));
    private readonly SoundSpecifier _endSound = new SoundPathSpecifier(new ResPath("/Audio/Effects/eye_close.ogg"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMagicVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CEMagicVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CEMagicVisionComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnStartup(Entity<CEMagicVisionComponent> ent, ref ComponentStartup args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        SyncOverlay(ent);
        _audio.PlayGlobal(_startSound, ent.Owner);
    }

    private void OnShutdown(Entity<CEMagicVisionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        RemoveOverlay();
        _audio.PlayGlobal(_endSound, ent.Owner);
    }

    private void OnHandleState(Entity<CEMagicVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        SyncOverlay(ent);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (TryComp<CEMagicVisionComponent>(args.Entity, out var comp))
            SyncOverlay((args.Entity, comp));
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    private void SyncOverlay(Entity<CEMagicVisionComponent> ent)
    {
        if (!ent.Comp.ShowOverlay)
        {
            RemoveOverlay();
            return;
        }

        if (_overlay == null)
        {
            _overlay = new CEMagicVisionOverlay();
            _overlayMan.AddOverlay(_overlay);
        }

        _overlay.StartOverlay = _timing.CurTime;
    }

    private void RemoveOverlay()
    {
        if (_overlay == null)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _overlay = null;
    }
}
