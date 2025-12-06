using Content.Server.Audio;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Drill;

namespace Content.Server._CE.Drill;

public sealed class CEDrillSystem : CESharedDrillSystem
{
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEDrillComponent, PowerConsumerReceivedChanged>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<CEDrillComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        var enabled = args.ReceivedPower >= args.DrawRate;
        _ambient.SetAmbience(ent,  enabled);
        ent.Comp.Enabled = enabled;
        Dirty(ent);
    }
}
