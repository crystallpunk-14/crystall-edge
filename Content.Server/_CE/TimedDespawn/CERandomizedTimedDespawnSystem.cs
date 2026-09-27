using Content.Shared._CE.TimedDespawn;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server._CE.TimedDespawn;

public sealed partial class CERandomizedTimedDespawnSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    /// <summary>
    /// Rolls the entity's <see cref="CERandomizedTimedDespawnComponent.Lifetime"/> between its Min/Max
    /// bounds and applies it to <see cref="TimedDespawnComponent"/>, adding one if it isn't already
    /// present.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CERandomizedTimedDespawnComponent> ent, ref MapInitEvent args)
    {
        var minSeconds = (float)ent.Comp.MinLifetime.TotalSeconds;
        var maxSeconds = (float)ent.Comp.MaxLifetime.TotalSeconds;
        var lifetime = TimeSpan.FromSeconds(_random.NextFloat(minSeconds, maxSeconds));

        ent.Comp.SpawnTime = _timing.CurTime;
        ent.Comp.Lifetime = lifetime;

        EnsureComp<TimedDespawnComponent>(ent).Lifetime = (float)lifetime.TotalSeconds;

        Dirty(ent);
    }
}
