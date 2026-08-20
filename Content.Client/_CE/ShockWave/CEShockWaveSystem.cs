using Content.Shared._CE.ShockWave;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;

namespace Content.Client._CE.ShockWave;

public sealed partial class CEShockWaveSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private CEShockWaveOverlay _shockWaveOverlay = default!;

    // Guards against double-registering the same entity's wave (ComponentStartup fires once,
    // but AfterAutoHandleStateEvent can additionally fire later if the fields ever change).
    private readonly HashSet<EntityUid> _registered = new();

    public override void Initialize()
    {
        base.Initialize();

        _shockWaveOverlay = new CEShockWaveOverlay();
        _overlay.AddOverlay(_shockWaveOverlay);

        // ComponentStartup always fires for a newly-created component, regardless of whether the
        // engine actually sent a network state for it (it won't, if the values match what the
        // client would already derive from the entity's prototype). By this point the component's
        // fields already hold their correct values either way, so this is the reliable place to
        // register the wave.
        SubscribeLocalEvent<CEShockWaveComponent, ComponentStartup>(OnShockWaveStartup);
        // Fallback in case the fields are changed on an already-existing entity after creation.
        SubscribeLocalEvent<CEShockWaveComponent, AfterAutoHandleStateEvent>(OnShockWaveStateHandled);
        SubscribeLocalEvent<CEShockWaveComponent, ComponentRemove>(OnShockWaveRemoved);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<CEShockWaveOverlay>();
        _registered.Clear();
    }

    private void OnShockWaveStartup(Entity<CEShockWaveComponent> ent, ref ComponentStartup args)
    {
        RegisterWave(ent);
    }

    private void OnShockWaveStateHandled(Entity<CEShockWaveComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RegisterWave(ent);
    }

    private void RegisterWave(Entity<CEShockWaveComponent> ent)
    {
        // Only register the wave the first time we see it.
        if (!_registered.Add(ent.Owner))
            return;

        var xform = Transform(ent.Owner);
        if (xform.MapID == Robust.Shared.Map.MapId.Nullspace)
            return;

        _shockWaveOverlay.AddWave(
            _xform.GetWorldPosition(ent.Owner),
            xform.MapID,
            ent.Comp.FalloffPower,
            ent.Comp.Sharpness,
            ent.Comp.Width,
            ent.Comp.Duration
        );
    }

    private void OnShockWaveRemoved(Entity<CEShockWaveComponent> ent, ref ComponentRemove args)
    {
        _registered.Remove(ent.Owner);
    }
}

