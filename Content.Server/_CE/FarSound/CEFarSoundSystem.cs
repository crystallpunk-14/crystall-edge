using Content.Server.Explosion.EntitySystems;
using Content.Shared._CE.FarSound;
using Content.Shared.Trigger;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._CE.FarSound;

public sealed class CEFarSoundSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEFarSoundComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<CEFarSoundComponent> ent, ref TriggerEvent args)
    {
        var mapPos =  _transform.GetMapCoordinates(ent);
        var entPos = Transform(ent).Coordinates;
        //Play close  sound
        _audio.PlayPvs(ent.Comp.CloseSound, entPos);

        //Play far sound
        var farFilter = Filter.Empty().AddInRange(mapPos, ent.Comp.FarRange);

        _audio.PlayGlobal(ent.Comp.FarSound, farFilter, true);
    }
}
