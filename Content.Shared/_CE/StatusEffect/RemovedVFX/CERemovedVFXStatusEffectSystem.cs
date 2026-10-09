using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._CE.StatusEffect.RemovedVFX;

public sealed partial class CERemovedVFXStatusEffectSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CERemovedVFXStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_net.IsClient || TerminatingOrDeleted(args.Target))
            return;

        var coordinates = Transform(args.Target).Coordinates;

        if (ent.Comp.Vfx is { } vfx)
            SpawnAtPosition(vfx, coordinates);

        _audio.PlayPvs(ent.Comp.Sound, coordinates);
    }
}
