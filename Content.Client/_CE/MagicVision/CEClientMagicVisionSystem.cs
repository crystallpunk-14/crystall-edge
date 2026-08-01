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
    private CEMagicVisionNoirOverlay? _noirOverlay;

    private readonly SoundSpecifier _startSound = new SoundPathSpecifier(new ResPath("/Audio/Effects/eye_open.ogg"));
    private readonly SoundSpecifier _endSound = new SoundPathSpecifier(new ResPath("/Audio/Effects/eye_close.ogg"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMagicVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CEMagicVisionComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnStartup(Entity<CEMagicVisionComponent> ent, ref ComponentStartup args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        ApplyOverlay(ent);
    }

    private void OnShutdown(Entity<CEMagicVisionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        RemoveOverlay(ent);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (TryComp<CEMagicVisionComponent>(args.Entity, out var comp))
            ApplyOverlay((args.Entity, comp));
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveOverlay(null);
    }

    private void ApplyOverlay(Entity<CEMagicVisionComponent> ent)
    {
        if (_overlay == null)
        {
            _overlay = new CEMagicVisionOverlay();
            _overlayMan.AddOverlay(_overlay);
        }

        _overlay.StartOverlay = _timing.CurTime;

        // TEMP: noir/color-correction overlay disabled, re-enable this block to restore it
        // if (_noirOverlay == null)
        // {
        //     _noirOverlay = new CEMagicVisionNoirOverlay();
        //     _overlayMan.AddOverlay(_noirOverlay);
        // }

        _audio.PlayGlobal(_startSound, ent.Owner);
    }

    private void RemoveOverlay(Entity<CEMagicVisionComponent>? ent)
    {
        if (_overlay == null && _noirOverlay == null)
            return;

        if (_overlay != null)
        {
            _overlayMan.RemoveOverlay(_overlay);
            _overlay = null;
        }

        if (_noirOverlay != null)
        {
            _overlayMan.RemoveOverlay(_noirOverlay);
            _noirOverlay = null;
        }

        if (ent is { } target)
            _audio.PlayGlobal(_endSound, target.Owner);
    }
}
