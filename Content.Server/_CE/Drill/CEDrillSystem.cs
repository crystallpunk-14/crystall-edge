using Content.Server.Audio;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Drill;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Drill;

/// <inheritdoc/>
public sealed class CEDrillSystem : CESharedDrillSystem
{
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEDrillComponent, PowerConsumerReceivedChanged>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<CEDrillComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        var enabled = args.ReceivedPower >= args.DrawRate;
        _ambient.SetAmbience(ent, enabled);
        ent.Comp.Enabled = enabled;

        ent.Comp.NextDamageTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextDouble(0, 1));
        Dirty(ent);
    }
}
