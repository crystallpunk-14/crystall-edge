using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.StatusEffect.VFX;

public sealed partial class CEVFXStatusEffectSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<CEVFXStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        Play(args.Target, ent.Comp.AppliedVfx, ent.Comp.AppliedSound);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CEVFXStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        Play(args.Target, ent.Comp.RemovedVfx, ent.Comp.RemovedSound);
    }

    private void Play(EntityUid target, EntProtoId? vfx, SoundSpecifier? sound)
    {
        if (_net.IsClient || TerminatingOrDeleted(target))
            return;

        var coordinates = Transform(target).Coordinates;

        if (vfx is { } vfxProto)
            SpawnAtPosition(vfxProto, coordinates);

        _audio.PlayPvs(sound, coordinates);
    }
}
